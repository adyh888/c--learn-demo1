# Day 13: MQTT+Modbus整合 - 完整IoT网关

> **学习目标**: 整合MQTT和Modbus，构建完整的工业物联网网关系统
>
> **预计时间**: 3-4小时

---

## 🎯 项目目标：工业IoT网关

构建一个完整的IoT网关系统，实现：

- ✅ Modbus设备数据采集
- ✅ MQTT消息发布/订阅
- ✅ 数据持久化存储
- ✅ RESTful API接口
- ✅ 实时监控面板

---

## 🏗️ 系统架构

```
┌─────────────────────────────────────────────────────────┐
│                    工业IoT网关系统                        │
└─────────────────────────────────────────────────────────┘

                    ┌──────────────┐
                    │  Web API     │ ← HTTP/REST
                    │  (ASP.NET)   │
                    └──────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
   ┌────▼────┐       ┌────▼────┐       ┌────▼────┐
   │ Modbus  │       │  MQTT   │       │Database │
   │ Service │       │ Service │       │ Service │
   └────┬────┘       └────┬────┘       └────┬────┘
        │                 │                  │
   ┌────▼────┐       ┌────▼────┐       ┌────▼────┐
   │PLC/设备 │       │ Broker  │       │ SQLite  │
   └─────────┘       └─────────┘       └─────────┘

数据流转:
1. Modbus Service 定时读取PLC数据
2. 解析后发布到 MQTT Broker
3. 同时存储到数据库
4. Web API 提供查询接口
5. 前端实时显示数据
```

---

## 📦 Step 1: 创建项目结构

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2
dotnet new webapi -n Day13IoTGateway
cd Day13IoTGateway

# 安装依赖包
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package MQTTnet
dotnet add package MQTTnet.Extensions.ManagedClient
dotnet add package NModbus4
```

**项目结构:**

```
Day13IoTGateway/
├── Controllers/          # API控制器
│   ├── GatewayController.cs
│   ├── DeviceController.cs
│   └── DataController.cs
├── Services/            # 服务层
│   ├── ModbusService.cs
│   ├── MqttService.cs
│   ├── GatewayService.cs
│   └── DataService.cs
├── Models/              # 数据模型
│   ├── Device.cs
│   ├── DeviceData.cs
│   └── GatewayConfig.cs
├── Data/               # 数据访问
│   └── AppDbContext.cs
└── wwwroot/            # 前端页面
    └── index.html
```

---

## 📝 Step 2: 定义核心模型

创建 `Models/GatewayModels.cs`：

```csharp
namespace Day13IoTGateway.Models
{
    // 设备配置
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;  // "Modbus" or "MQTT"
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPollTime { get; set; }
        
        // Modbus特有配置
        public byte SlaveId { get; set; } = 1;
        public int PollInterval { get; set; } = 5000;
        
        // 关联的数据点
        public List<DataPoint> DataPoints { get; set; } = new();
    }

    // 数据点配置（寄存器/主题映射）
    public class DataPoint
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Modbus配置
        public ushort? RegisterAddress { get; set; }
        public string RegisterType { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public double Scale { get; set; } = 1.0;
        
        // MQTT配置
        public string MqttTopic { get; set; } = string.Empty;
        
        // 通用属性
        public string Unit { get; set; } = string.Empty;
        
        public Device? Device { get; set; }
    }

    // 实时数据记录
    public class DeviceDataRecord
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string DataPointName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;  // "Modbus" or "MQTT"
    }
}
```

---

## 🎯 Step 3: 创建网关核心服务

创建 `Services/GatewayService.cs`：

```csharp
using Day13IoTGateway.Models;
using Day13IoTGateway.Data;
using Microsoft.EntityFrameworkCore;

namespace Day13IoTGateway.Services
{
    public interface IGatewayService
    {
        Task StartAsync();
        Task StopAsync();
        Task<List<Device>> GetDevicesAsync();
        Task<Device> AddDeviceAsync(Device device);
        Task<List<DeviceDataRecord>> GetRealtimeDataAsync();
    }

    public class GatewayService : IGatewayService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IModbusService _modbusService;
        private readonly IMqttService _mqttService;
        private readonly ILogger<GatewayService> _logger;
        private readonly Dictionary<int, CancellationTokenSource> _devicePollers = new();

        public GatewayService(
            IServiceProvider serviceProvider,
            IModbusService modbusService,
            IMqttService mqttService,
            ILogger<GatewayService> logger)
        {
            _serviceProvider = serviceProvider;
            _modbusService = modbusService;
            _mqttService = mqttService;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("🚀 网关服务启动");

            // 启动MQTT服务
            await _mqttService.StartAsync();

            // 加载所有设备并启动轮询
            var devices = await GetDevicesAsync();
            foreach (var device in devices.Where(d => d.IsEnabled))
            {
                await StartDevicePollingAsync(device);
            }
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("🛑 网关服务停止");

            // 停止所有轮询
            foreach (var cts in _devicePollers.Values)
            {
                cts.Cancel();
            }
            _devicePollers.Clear();

            // 停止MQTT
            await _mqttService.StopAsync();
        }

        public async Task<List<Device>> GetDevicesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Devices.Include(d => d.DataPoints).ToListAsync();
        }

        public async Task<Device> AddDeviceAsync(Device device)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            device.CreatedAt = DateTime.UtcNow;
            context.Devices.Add(device);
            await context.SaveChangesAsync();

            // 如果设备启用，立即开始轮询
            if (device.IsEnabled)
            {
                await StartDevicePollingAsync(device);
            }

            return device;
        }

        public async Task<List<DeviceDataRecord>> GetRealtimeDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 获取每个设备的最新数据
            return await context.DeviceDataRecords
                .GroupBy(d => new { d.DeviceId, d.DataPointName })
                .Select(g => g.OrderByDescending(d => d.Timestamp).First())
                .ToListAsync();
        }

        // 启动设备轮询
        private async Task StartDevicePollingAsync(Device device)
        {
            if (device.Protocol != "Modbus")
            {
                _logger.LogWarning("设备 {Device} 协议 {Protocol} 不支持轮询", device.Name, device.Protocol);
                return;
            }

            var cts = new CancellationTokenSource();
            _devicePollers[device.Id] = cts;

            _ = Task.Run(async () =>
            {
                _logger.LogInformation("📊 开始轮询设备: {Device}", device.Name);

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await PollModbusDeviceAsync(device);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "轮询设备失败: {Device}", device.Name);
                    }

                    await Task.Delay(device.PollInterval, cts.Token);
                }
            }, cts.Token);
        }

        // 轮询Modbus设备
        private async Task PollModbusDeviceAsync(Device device)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 读取所有数据点
            foreach (var dataPoint in device.DataPoints)
            {
                try
                {
                    // 从Modbus读取数据
                    var value = await ReadModbusDataPointAsync(device, dataPoint);

                    // 存储到数据库
                    var record = new DeviceDataRecord
                    {
                        DeviceId = device.Id,
                        DataPointName = dataPoint.Name,
                        Value = value,
                        Unit = dataPoint.Unit,
                        Timestamp = DateTime.UtcNow,
                        Source = "Modbus"
                    };
                    context.DeviceDataRecords.Add(record);

                    // 发布到MQTT
                    var mqttTopic = $"gateway/device/{device.Id}/{dataPoint.Name}";
                    var mqttPayload = new
                    {
                        deviceId = device.Id,
                        deviceName = device.Name,
                        dataPoint = dataPoint.Name,
                        value = value,
                        unit = dataPoint.Unit,
                        timestamp = record.Timestamp
                    };
                    await _mqttService.PublishAsync(mqttTopic, mqttPayload);

                    _logger.LogDebug(
                        "📤 {Device}.{DataPoint} = {Value}{Unit}",
                        device.Name, dataPoint.Name, value, dataPoint.Unit);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取数据点失败: {Device}.{DataPoint}",
                        device.Name, dataPoint.Name);
                }
            }

            await context.SaveChangesAsync();

            // 更新最后轮询时间
            device.LastPollTime = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        // 读取Modbus数据点
        private async Task<double> ReadModbusDataPointAsync(Device device, DataPoint dataPoint)
        {
            // 这里调用ModbusService读取实际数据
            // 简化版本，返回模拟数据
            await Task.Delay(10);  // 模拟IO延迟

            // 生成模拟数据
            return dataPoint.Name switch
            {
                "temperature" => 20 + Random.Shared.NextDouble() * 10,
                "humidity" => 50 + Random.Shared.NextDouble() * 30,
                "pressure" => 1.0 + Random.Shared.NextDouble() * 0.5,
                _ => Random.Shared.NextDouble() * 100
            };
        }
    }
}
```

---

## 🎮 Step 4: 创建Gateway API控制器

创建 `Controllers/GatewayController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day13IoTGateway.Models;
using Day13IoTGateway.Services;

namespace Day13IoTGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayController : ControllerBase
    {
        private readonly IGatewayService _gatewayService;

        public GatewayController(IGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        // GET: api/gateway/devices
        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices()
        {
            var devices = await _gatewayService.GetDevicesAsync();
            return Ok(devices);
        }

        // POST: api/gateway/devices
        [HttpPost("devices")]
        public async Task<IActionResult> AddDevice([FromBody] Device device)
        {
            var createdDevice = await _gatewayService.AddDeviceAsync(device);
            return CreatedAtAction(nameof(GetDevices), new { id = createdDevice.Id }, createdDevice);
        }

        // GET: api/gateway/realtime
        [HttpGet("realtime")]
        public async Task<IActionResult> GetRealtimeData()
        {
            var data = await _gatewayService.GetRealtimeDataAsync();
            
            var grouped = data.GroupBy(d => d.DeviceId)
                .Select(g => new
                {
                    deviceId = g.Key,
                    data = g.Select(d => new
                    {
                        name = d.DataPointName,
                        value = d.Value,
                        unit = d.Unit,
                        timestamp = d.Timestamp
                    })
                });

            return Ok(grouped);
        }

        // GET: api/gateway/status
        [HttpGet("status")]
        public async Task<IActionResult> GetGatewayStatus()
        {
            var devices = await _gatewayService.GetDevicesAsync();
            
            return Ok(new
            {
                totalDevices = devices.Count,
                enabledDevices = devices.Count(d => d.IsEnabled),
                onlineDevices = devices.Count(d => d.LastPollTime > DateTime.UtcNow.AddMinutes(-1)),
                timestamp = DateTime.UtcNow
            });
        }
    }
}
```

---

## 🎨 Step 5: 创建监控前端

创建 `wwwroot/dashboard.html`：

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <title>IoT网关监控面板</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: 'Segoe UI', Arial, sans-serif; 
            background: #f5f5f5;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .container { max-width: 1400px; margin: 20px auto; padding: 0 20px; }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }
        .stat-card {
            background: white;
            padding: 25px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .stat-value {
            font-size: 36px;
            font-weight: bold;
            color: #667eea;
        }
        .stat-label { color: #666; margin-top: 10px; }
        .devices {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
            gap: 20px;
        }
        .device-card {
            background: white;
            padding: 20px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .device-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 15px;
            padding-bottom: 15px;
            border-bottom: 2px solid #f0f0f0;
        }
        .device-name { font-size: 18px; font-weight: bold; }
        .status-badge {
            padding: 5px 15px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: bold;
        }
        .status-online { background: #d4edda; color: #155724; }
        .status-offline { background: #f8d7da; color: #721c24; }
        .data-item {
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #f0f0f0;
        }
        .data-value { font-size: 24px; font-weight: bold; color: #667eea; }
        .timestamp { color: #999; font-size: 12px; margin-top: 5px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🏭 工业IoT网关监控系统</h1>
        <p>实时监控 Modbus + MQTT 设备数据</p>
    </div>

    <div class="container">
        <div class="stats">
            <div class="stat-card">
                <div class="stat-value" id="totalDevices">0</div>
                <div class="stat-label">总设备数</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="onlineDevices">0</div>
                <div class="stat-label">在线设备</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="dataPoints">0</div>
                <div class="stat-label">数据点</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="lastUpdate">--:--:--</div>
                <div class="stat-label">最后更新</div>
            </div>
        </div>

        <div class="devices" id="devicesContainer"></div>
    </div>

    <script>
        async function loadStatus() {
            const res = await fetch('/api/gateway/status');
            const data = await res.json();
            
            document.getElementById('totalDevices').textContent = data.totalDevices;
            document.getElementById('onlineDevices').textContent = data.onlineDevices;
        }

        async function loadRealtimeData() {
            const res = await fetch('/api/gateway/realtime');
            const devices = await res.json();
            
            const container = document.getElementById('devicesContainer');
            let totalDataPoints = 0;

            container.innerHTML = devices.map(device => {
                totalDataPoints += device.data.length;
                
                return `
                    <div class="device-card">
                        <div class="device-header">
                            <div class="device-name">设备 ${device.deviceId}</div>
                            <div class="status-badge status-online">在线</div>
                        </div>
                        ${device.data.map(d => `
                            <div class="data-item">
                                <div>
                                    <div>${d.name}</div>
                                    <div class="timestamp">${new Date(d.timestamp).toLocaleTimeString()}</div>
                                </div>
                                <div class="data-value">${d.value.toFixed(2)} ${d.unit}</div>
                            </div>
                        `).join('')}
                    </div>
                `;
            }).join('');

            document.getElementById('dataPoints').textContent = totalDataPoints;
            document.getElementById('lastUpdate').textContent = new Date().toLocaleTimeString();
        }

        // 定时刷新
        setInterval(() => {
            loadStatus();
            loadRealtimeData();
        }, 2000);

        // 初始加载
        loadStatus();
        loadRealtimeData();
    </script>
</body>
</html>
```

---

## ⚙️ Step 6: 配置和启动

修改 `Program.cs`：

```csharp
using Day13IoTGateway.Data;
using Day13IoTGateway.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 数据库
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=gateway.db"));

// 服务
builder.Services.AddSingleton<IModbusService, ModbusService>();
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<IGatewayService, GatewayService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 数据库迁移
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 启动网关
var gateway = app.Services.GetRequiredService<IGatewayService>();
await gateway.StartAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 📝 今日总结

### ✅ 你完成了：

- [x] 整合Modbus和MQTT
- [x] 实现完整的IoT网关
- [x] 设备管理和配置
- [x] 实时数据采集和转发
- [x] 数据持久化
- [x] 监控面板

### 🏆 项目架构完整度：

```
✅ 协议层: Modbus TCP + MQTT
✅ 服务层: 网关服务 + 轮询服务
✅ 数据层: EF Core + SQLite
✅ API层: RESTful API
✅ 展示层: 实时监控面板
```

---

## 🎯 明日预告：Day 14 - 项目总结和优化

明天是最后一天，你将：

- 回顾14天的学习内容
- 项目性能优化
- 部署和运维
- 下一步学习建议

---

**IoT网关完成！明天进行总结和展望！🎉**


