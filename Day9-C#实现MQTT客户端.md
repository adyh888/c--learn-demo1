# Day 9: C#实现MQTT客户端

> **学习目标**: 使用MQTTnet库实现MQTT发布/订阅功能
> 
> **预计时间**: 2-3小时

---

## 🚀 Step 1: 创建项目并安装MQTTnet

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2
dotnet new webapi -n Day9MqttAPI
cd Day9MqttAPI

# 安装MQTTnet包
dotnet add package MQTTnet
dotnet add package MQTTnet.Extensions.ManagedClient
```

---

## 📦 Step 2: 创建MQTT服务

创建 `Services/MqttService.cs`：

```csharp
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;

namespace Day9MqttAPI.Services
{
    public interface IMqttService
    {
        Task StartAsync();
        Task StopAsync();
        Task PublishAsync(string topic, object payload);
        Task SubscribeAsync(string topic);
        Task UnsubscribeAsync(string topic);
        bool IsConnected { get; }
    }

    public class MqttService : IMqttService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IManagedMqttClient _mqttClient;
        private readonly ManagedMqttClientOptions _options;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public MqttService(ILogger<MqttService> logger, IConfiguration configuration)
        {
            _logger = logger;

            // 创建MQTT客户端
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            // 配置连接选项
            var mqttConfig = configuration.GetSection("Mqtt");
            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttConfig["Server"], int.Parse(mqttConfig["Port"]))
                .WithClientId(mqttConfig["ClientId"] ?? Guid.NewGuid().ToString())
                .WithCleanSession()
                .Build();

            _options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptions)
                .Build();

            // 设置事件处理
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("启动MQTT客户端...");
            await _mqttClient.StartAsync(_options);
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("停止MQTT客户端...");
            await _mqttClient.StopAsync();
        }

        public async Task PublishAsync(string topic, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.EnqueueAsync(message);
            _logger.LogInformation("发布消息到主题 {Topic}: {Payload}", topic, json);
        }

        public async Task SubscribeAsync(string topic)
        {
            await _mqttClient.SubscribeAsync(topic);
            _logger.LogInformation("订阅主题: {Topic}", topic);
        }

        public async Task UnsubscribeAsync(string topic)
        {
            await _mqttClient.UnsubscribeAsync(topic);
            _logger.LogInformation("取消订阅主题: {Topic}", topic);
        }

        // 连接成功事件
        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation("✅ MQTT客户端已连接");
            return Task.CompletedTask;
        }

        // 断开连接事件
        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogWarning("❌ MQTT客户端断开连接: {Reason}", e.Reason);
            return Task.CompletedTask;
        }

        // 收到消息事件
        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            _logger.LogInformation(
                "📨 收到消息 - 主题: {Topic}, 负载: {Payload}",
                e.ApplicationMessage.Topic,
                payload);
            return Task.CompletedTask;
        }
    }
}
```

**🔵 与前端对比:**

```javascript
// JavaScript MQTT客户端
const mqtt = require('mqtt');
const client = mqtt.connect('mqtt://broker.emqx.io:1883');

client.on('connect', () => {
  console.log('✅ 已连接');
});

client.on('message', (topic, message) => {
  console.log('📨 收到消息:', topic, message.toString());
});

client.subscribe('test/topic');
client.publish('test/topic', JSON.stringify({data: 'hello'}));
```

---

## ⚙️ Step 3: 配置MQTT连接

修改 `appsettings.json`：

```json
{
  "Mqtt": {
    "Server": "broker.emqx.io",
    "Port": 1883,
    "ClientId": "dotnet-iot-backend"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 🎮 Step 4: 创建MQTT控制器

创建 `Controllers/MqttController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day9MqttAPI.Services;

namespace Day9MqttAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MqttController : ControllerBase
    {
        private readonly IMqttService _mqttService;
        private readonly ILogger<MqttController> _logger;

        public MqttController(IMqttService mqttService, ILogger<MqttController> logger)
        {
            _mqttService = mqttService;
            _logger = logger;
        }

        // GET: api/mqtt/status
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                isConnected = _mqttService.IsConnected,
                timestamp = DateTime.Now
            });
        }

        // POST: api/mqtt/publish
        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] PublishRequest request)
        {
            if (!_mqttService.IsConnected)
            {
                return BadRequest(new { error = "MQTT客户端未连接" });
            }

            await _mqttService.PublishAsync(request.Topic, request.Payload);

            return Ok(new
            {
                message = "消息已发布",
                topic = request.Topic,
                timestamp = DateTime.Now
            });
        }

        // POST: api/mqtt/subscribe
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            if (!_mqttService.IsConnected)
            {
                return BadRequest(new { error = "MQTT客户端未连接" });
            }

            await _mqttService.SubscribeAsync(request.Topic);

            return Ok(new
            {
                message = "订阅成功",
                topic = request.Topic
            });
        }

        // POST: api/mqtt/unsubscribe
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscribeRequest request)
        {
            await _mqttService.UnsubscribeAsync(request.Topic);

            return Ok(new
            {
                message = "取消订阅成功",
                topic = request.Topic
            });
        }
    }

    // 请求模型
    public class PublishRequest
    {
        public string Topic { get; set; } = string.Empty;
        public object Payload { get; set; } = new { };
    }

    public class SubscribeRequest
    {
        public string Topic { get; set; } = string.Empty;
    }
}
```

---

## 🏃 Step 5: 启动MQTT服务

修改 `Program.cs`：

```csharp
using Day9MqttAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册MQTT服务为单例
builder.Services.AddSingleton<IMqttService, MqttService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 启动MQTT客户端
var mqttService = app.Services.GetRequiredService<IMqttService>();
await mqttService.StartAsync();

// 订阅默认主题
await mqttService.SubscribeAsync("factory/+/temperature");
await mqttService.SubscribeAsync("factory/+/humidity");

app.Run();
```

---

## 🧪 Step 6: 测试MQTT功能

### 测试1: 发布消息

```bash
curl -X POST http://localhost:5000/api/mqtt/publish \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "factory/workshop1/temperature",
    "payload": {
      "deviceId": "sensor-001",
      "value": 25.5,
      "unit": "°C",
      "timestamp": "2025-10-15T10:30:00Z"
    }
  }'
```

### 测试2: 订阅主题

```bash
curl -X POST http://localhost:5000/api/mqtt/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "factory/workshop2/humidity"
  }'
```

### 测试3: 查看状态

```bash
curl http://localhost:5000/api/mqtt/status
```

### 测试4: 使用MQTTX验证

1. 打开MQTTX工具
2. 连接到同一个Broker: `broker.emqx.io:1883`
3. 订阅主题: `factory/#`
4. 通过API发布消息
5. 在MQTTX中看到消息

---

## 🎯 Step 7: 集成设备数据（实战）

创建 `Services/DeviceMqttService.cs`：

```csharp
using Day9MqttAPI.Models;

namespace Day9MqttAPI.Services
{
    public interface IDeviceMqttService
    {
        Task PublishDeviceDataAsync(int deviceId, DeviceDataMessage data);
        Task PublishDeviceStatusAsync(int deviceId, string status);
    }

    public class DeviceMqttService : IDeviceMqttService
    {
        private readonly IMqttService _mqttService;
        private readonly ILogger<DeviceMqttService> _logger;

        public DeviceMqttService(
            IMqttService mqttService,
            ILogger<DeviceMqttService> logger)
        {
            _mqttService = mqttService;
            _logger = logger;
        }

        public async Task PublishDeviceDataAsync(int deviceId, DeviceDataMessage data)
        {
            var topic = $"factory/device/{deviceId}/data";
            await _mqttService.PublishAsync(topic, data);
            _logger.LogInformation("发布设备数据: Device={DeviceId}, Topic={Topic}", deviceId, topic);
        }

        public async Task PublishDeviceStatusAsync(int deviceId, string status)
        {
            var topic = $"factory/device/{deviceId}/status";
            var payload = new
            {
                deviceId = deviceId,
                status = status,
                timestamp = DateTime.UtcNow
            };
            await _mqttService.PublishAsync(topic, payload);
            _logger.LogInformation("发布设备状态: Device={DeviceId}, Status={Status}", deviceId, status);
        }
    }
}
```

创建 `Models/DeviceDataMessage.cs`：

```csharp
namespace Day9MqttAPI.Models
{
    public class DeviceDataMessage
    {
        public int DeviceId { get; set; }
        public string DataType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
```

创建 `Controllers/DeviceDataController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day9MqttAPI.Models;
using Day9MqttAPI.Services;

namespace Day9MqttAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceDataController : ControllerBase
    {
        private readonly IDeviceMqttService _deviceMqttService;

        public DeviceDataController(IDeviceMqttService deviceMqttService)
        {
            _deviceMqttService = deviceMqttService;
        }

        // POST: api/devicedata/report
        [HttpPost("report")]
        public async Task<IActionResult> ReportData([FromBody] DeviceDataMessage data)
        {
            data.Timestamp = DateTime.UtcNow;
            await _deviceMqttService.PublishDeviceDataAsync(data.DeviceId, data);

            return Ok(new
            {
                message = "数据已发布到MQTT",
                data = data
            });
        }

        // POST: api/devicedata/status
        [HttpPost("{deviceId}/status")]
        public async Task<IActionResult> UpdateStatus(int deviceId, [FromBody] StatusUpdate update)
        {
            await _deviceMqttService.PublishDeviceStatusAsync(deviceId, update.Status);

            return Ok(new
            {
                message = "状态已更新",
                deviceId = deviceId,
                status = update.Status
            });
        }
    }

    public class StatusUpdate
    {
        public string Status { get; set; } = string.Empty;
    }
}
```

---

## 🔥 Step 8: 后台服务自动发布数据

创建 `Services/DeviceSimulatorService.cs`：

```csharp
namespace Day9MqttAPI.Services
{
    // 后台服务：模拟设备定时上报数据
    public class DeviceSimulatorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeviceSimulatorService> _logger;

        public DeviceSimulatorService(
            IServiceProvider serviceProvider,
            ILogger<DeviceSimulatorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("设备模拟器启动");

            // 等待5秒让MQTT连接完成
            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var deviceMqtt = scope.ServiceProvider.GetRequiredService<IDeviceMqttService>();

                    // 模拟3个设备上报数据
                    for (int deviceId = 1; deviceId <= 3; deviceId++)
                    {
                        var data = new Models.DeviceDataMessage
                        {
                            DeviceId = deviceId,
                            DataType = "temperature",
                            Value = 20 + Random.Shared.NextDouble() * 10,  // 20-30°C
                            Unit = "°C",
                            Timestamp = DateTime.UtcNow
                        };

                        await deviceMqtt.PublishDeviceDataAsync(deviceId, data);
                    }

                    _logger.LogInformation("✅ 模拟设备数据已发布");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发布模拟数据失败");
                }

                // 每10秒发布一次
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
```

**注册后台服务（Program.cs）：**

```csharp
// 注册后台服务
builder.Services.AddHostedService<DeviceSimulatorService>();

// 注册设备MQTT服务
builder.Services.AddSingleton<IDeviceMqttService, DeviceMqttService>();
```

---

## 📊 Step 9: 创建简单的前端监控页面

创建 `wwwroot/index.html`：

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <title>MQTT设备监控</title>
    <style>
        body { font-family: Arial; max-width: 1200px; margin: 50px auto; }
        .device-card {
            border: 1px solid #ddd;
            padding: 20px;
            margin: 10px;
            border-radius: 5px;
            display: inline-block;
            width: 300px;
        }
        .value { font-size: 32px; font-weight: bold; color: #007bff; }
        button { padding: 10px 20px; margin: 5px; cursor: pointer; }
    </style>
    <script src="https://unpkg.com/mqtt/dist/mqtt.min.js"></script>
</head>
<body>
    <h1>🏭 MQTT设备实时监控</h1>
    
    <div>
        <button onclick="connectMqtt()">连接MQTT</button>
        <button onclick="publishTest()">发布测试消息</button>
        <span id="status">未连接</span>
    </div>

    <div id="devices"></div>

    <script>
        let client = null;
        const devices = {};

        function connectMqtt() {
            client = mqtt.connect('ws://broker.emqx.io:8083/mqtt');

            client.on('connect', () => {
                document.getElementById('status').textContent = '✅ 已连接';
                console.log('MQTT连接成功');

                // 订阅所有设备数据
                client.subscribe('factory/device/+/data');
                client.subscribe('factory/device/+/status');
            });

            client.on('message', (topic, message) => {
                const data = JSON.parse(message.toString());
                console.log('收到消息:', topic, data);

                if (topic.includes('/data')) {
                    updateDeviceData(data);
                }
            });
        }

        function updateDeviceData(data) {
            devices[data.deviceId] = data;
            renderDevices();
        }

        function renderDevices() {
            const container = document.getElementById('devices');
            container.innerHTML = Object.values(devices).map(device => `
                <div class="device-card">
                    <h3>设备 ${device.deviceId}</h3>
                    <div class="value">${device.value.toFixed(2)} ${device.unit}</div>
                    <div>${device.dataType}</div>
                    <div>时间: ${new Date(device.timestamp).toLocaleTimeString()}</div>
                </div>
            `).join('');
        }

        function publishTest() {
            if (!client) {
                alert('请先连接MQTT');
                return;
            }

            const message = {
                deviceId: 999,
                dataType: 'temperature',
                value: 25.5,
                unit: '°C',
                timestamp: new Date().toISOString()
            };

            client.publish('factory/device/999/data', JSON.stringify(message));
            console.log('发布测试消息');
        }

        // 页面加载时自动连接
        window.onload = () => {
            connectMqtt();
        };
    </script>
</body>
</html>
```

**启用静态文件（Program.cs）：**

```csharp
app.UseStaticFiles();
```

**访问**: `http://localhost:5000/index.html`

---

## 📝 今日总结

### ✅ 你学会了：
- [x] 安装和使用MQTTnet库
- [x] 创建MQTT客户端服务
- [x] 发布和订阅MQTT消息
- [x] 集成MQTT到ASP.NET Core
- [x] 创建后台服务自动发布数据
- [x] 前端实时监控MQTT消息

### 🔑 核心代码对比：

| 功能 | C# (MQTTnet) | JavaScript (mqtt.js) |
|-----|--------------|---------------------|
| 连接 | `mqttClient.StartAsync()` | `mqtt.connect()` |
| 发布 | `EnqueueAsync(message)` | `client.publish()` |
| 订阅 | `SubscribeAsync(topic)` | `client.subscribe()` |
| 接收 | `ApplicationMessageReceivedAsync` | `client.on('message')` |

---

## 🎯 明日预告：Day 10 - MQTT消息持久化

明天你将学习：
- MQTT消息存储到数据库
- 历史数据查询
- 数据聚合和统计
- 实时 + 历史数据结合

---

## 💾 作业

1. 实现MQTT消息的数据库持久化
2. 添加消息过滤（只存储特定类型的数据）
3. 实现设备在线/离线检测
4. 优化前端页面，添加图表显示

---

**MQTT客户端实现完成！明天继续完善！🚀**


