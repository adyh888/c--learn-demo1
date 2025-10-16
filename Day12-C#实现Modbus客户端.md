# Day 12: C#实现Modbus客户端

> **学习目标**: 使用NModbus库实现Modbus TCP通信
> 
> **预计时间**: 2-3小时

---

## 🚀 Step 1: 创建项目并安装NModbus

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2
dotnet new webapi -n Day12ModbusAPI
cd Day12ModbusAPI

# 安装NModbus4包
dotnet add package NModbus4
```

---

## 📦 Step 2: 创建Modbus配置模型

创建 `Models/ModbusDevice.cs`：

```csharp
namespace Day12ModbusAPI.Models
{
    // Modbus设备配置
    public class ModbusDeviceConfig
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 502;
        public byte SlaveId { get; set; } = 1;
        public int PollInterval { get; set; } = 5000;  // 轮询间隔(毫秒)
        public List<ModbusRegister> Registers { get; set; } = new();
    }

    // Modbus寄存器配置
    public class ModbusRegister
    {
        public string Name { get; set; } = string.Empty;        // 名称（如"温度"）
        public ushort Address { get; set; }                     // 寄存器地址
        public ModbusRegisterType Type { get; set; }            // 类型
        public ModbusDataType DataType { get; set; }            // 数据类型
        public double Scale { get; set; } = 1.0;                // 缩放系数
        public string Unit { get; set; } = string.Empty;        // 单位
    }

    // 寄存器类型
    public enum ModbusRegisterType
    {
        Coil = 1,              // 线圈
        DiscreteInput = 2,     // 离散输入
        InputRegister = 3,     // 输入寄存器
        HoldingRegister = 4    // 保持寄存器
    }

    // 数据类型
    public enum ModbusDataType
    {
        UInt16,    // 16位无符号整数（1个寄存器）
        Int16,     // 16位有符号整数（1个寄存器）
        UInt32,    // 32位无符号整数（2个寄存器）
        Int32,     // 32位有符号整数（2个寄存器）
        Float32    // 32位浮点数（2个寄存器）
    }

    // 读取结果
    public class ModbusReadResult
    {
        public string RegisterName { get; set; } = string.Empty;
        public object Value { get; set; } = 0;
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
```

---

## 🔧 Step 3: 创建Modbus服务

创建 `Services/ModbusService.cs`：

```csharp
using Modbus.Device;
using System.Net.Sockets;
using Day12ModbusAPI.Models;

namespace Day12ModbusAPI.Services
{
    public interface IModbusService
    {
        Task<ModbusReadResult> ReadRegisterAsync(
            ModbusDeviceConfig device,
            ModbusRegister register);
        
        Task<List<ModbusReadResult>> ReadAllRegistersAsync(
            ModbusDeviceConfig device);
        
        Task WriteRegisterAsync(
            ModbusDeviceConfig device,
            ushort address,
            ushort value);
    }

    public class ModbusService : IModbusService
    {
        private readonly ILogger<ModbusService> _logger;

        public ModbusService(ILogger<ModbusService> logger)
        {
            _logger = logger;
        }

        public async Task<ModbusReadResult> ReadRegisterAsync(
            ModbusDeviceConfig device,
            ModbusRegister register)
        {
            using var client = new TcpClient(device.IpAddress, device.Port);
            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);

            ushort[] data;

            // 根据寄存器类型读取
            switch (register.Type)
            {
                case ModbusRegisterType.HoldingRegister:
                    data = await master.ReadHoldingRegistersAsync(
                        device.SlaveId,
                        register.Address,
                        GetRegisterCount(register.DataType));
                    break;

                case ModbusRegisterType.InputRegister:
                    data = await master.ReadInputRegistersAsync(
                        device.SlaveId,
                        register.Address,
                        GetRegisterCount(register.DataType));
                    break;

                default:
                    throw new NotSupportedException($"寄存器类型 {register.Type} 暂不支持");
            }

            var value = ParseValue(data, register.DataType, register.Scale);

            return new ModbusReadResult
            {
                RegisterName = register.Name,
                Value = value,
                Unit = register.Unit,
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task<List<ModbusReadResult>> ReadAllRegistersAsync(
            ModbusDeviceConfig device)
        {
            var results = new List<ModbusReadResult>();

            foreach (var register in device.Registers)
            {
                try
                {
                    var result = await ReadRegisterAsync(device, register);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取寄存器失败: {RegisterName}", register.Name);
                }
            }

            return results;
        }

        public async Task WriteRegisterAsync(
            ModbusDeviceConfig device,
            ushort address,
            ushort value)
        {
            using var client = new TcpClient(device.IpAddress, device.Port);
            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);

            await master.WriteSingleRegisterAsync(device.SlaveId, address, value);

            _logger.LogInformation(
                "写入寄存器成功: Device={Device}, Address={Address}, Value={Value}",
                device.Name, address, value);
        }

        // 根据数据类型确定需要读取的寄存器数量
        private ushort GetRegisterCount(ModbusDataType dataType)
        {
            return dataType switch
            {
                ModbusDataType.UInt16 or ModbusDataType.Int16 => 1,
                ModbusDataType.UInt32 or ModbusDataType.Int32 or ModbusDataType.Float32 => 2,
                _ => 1
            };
        }

        // 解析寄存器值
        private object ParseValue(ushort[] data, ModbusDataType dataType, double scale)
        {
            return dataType switch
            {
                ModbusDataType.UInt16 => data[0] * scale,
                ModbusDataType.Int16 => (short)data[0] * scale,
                ModbusDataType.UInt32 => ((uint)data[0] << 16 | data[1]) * scale,
                ModbusDataType.Int32 => ((int)data[0] << 16 | data[1]) * scale,
                ModbusDataType.Float32 => ParseFloat32(data) * scale,
                _ => data[0] * scale
            };
        }

        private float ParseFloat32(ushort[] data)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(data[1] & 0xFF);
            bytes[1] = (byte)(data[1] >> 8);
            bytes[2] = (byte)(data[0] & 0xFF);
            bytes[3] = (byte)(data[0] >> 8);
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
```

**🔵 与前端对比:**

```javascript
// JavaScript 伪代码
class ModbusClient {
  async readRegister(device, address) {
    const socket = new Socket(device.ip, device.port);
    const request = {
      functionCode: 0x03,  // 读保持寄存器
      address: address,
      count: 1
    };
    const response = await socket.send(request);
    return response.data[0];
  }
}
```

---

## 🎮 Step 4: 创建Modbus控制器

创建 `Controllers/ModbusController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day12ModbusAPI.Models;
using Day12ModbusAPI.Services;

namespace Day12ModbusAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModbusController : ControllerBase
    {
        private readonly IModbusService _modbusService;
        private readonly ILogger<ModbusController> _logger;

        // 测试用的设备配置
        private readonly ModbusDeviceConfig _testDevice = new()
        {
            Id = 1,
            Name = "测试PLC",
            IpAddress = "127.0.0.1",  // 本地测试
            Port = 502,
            SlaveId = 1,
            Registers = new List<ModbusRegister>
            {
                new() {
                    Name = "温度",
                    Address = 0,
                    Type = ModbusRegisterType.HoldingRegister,
                    DataType = ModbusDataType.UInt16,
                    Scale = 0.1,  // 寄存器值 / 10
                    Unit = "°C"
                },
                new() {
                    Name = "湿度",
                    Address = 1,
                    Type = ModbusRegisterType.HoldingRegister,
                    DataType = ModbusDataType.UInt16,
                    Scale = 0.1,
                    Unit = "%"
                },
                new() {
                    Name = "压力",
                    Address = 2,
                    Type = ModbusRegisterType.HoldingRegister,
                    DataType = ModbusDataType.UInt16,
                    Scale = 0.01,
                    Unit = "bar"
                }
            }
        };

        public ModbusController(
            IModbusService modbusService,
            ILogger<ModbusController> logger)
        {
            _modbusService = modbusService;
            _logger = logger;
        }

        // GET: api/modbus/read
        [HttpGet("read")]
        public async Task<IActionResult> ReadAllRegisters()
        {
            try
            {
                var results = await _modbusService.ReadAllRegistersAsync(_testDevice);

                return Ok(new
                {
                    device = _testDevice.Name,
                    timestamp = DateTime.UtcNow,
                    data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取Modbus数据失败");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/modbus/read/{registerName}
        [HttpGet("read/{registerName}")]
        public async Task<IActionResult> ReadRegister(string registerName)
        {
            var register = _testDevice.Registers
                .FirstOrDefault(r => r.Name == registerName);

            if (register == null)
            {
                return NotFound(new { error = $"寄存器 {registerName} 不存在" });
            }

            try
            {
                var result = await _modbusService.ReadRegisterAsync(_testDevice, register);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取寄存器失败: {RegisterName}", registerName);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/modbus/write
        [HttpPost("write")]
        public async Task<IActionResult> WriteRegister([FromBody] WriteRequest request)
        {
            try
            {
                await _modbusService.WriteRegisterAsync(
                    _testDevice,
                    request.Address,
                    request.Value);

                return Ok(new
                {
                    message = "写入成功",
                    address = request.Address,
                    value = request.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写入寄存器失败");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class WriteRequest
    {
        public ushort Address { get; set; }
        public ushort Value { get; set; }
    }
}
```

---

## 🔄 Step 5: 创建Modbus轮询服务

创建 `Services/ModbusPollingService.cs`：

```csharp
using Day12ModbusAPI.Models;

namespace Day12ModbusAPI.Services
{
    // 后台服务：定时轮询Modbus设备
    public class ModbusPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModbusPollingService> _logger;

        // 测试设备配置
        private readonly ModbusDeviceConfig _device = new()
        {
            Id = 1,
            Name = "车间PLC-01",
            IpAddress = "127.0.0.1",
            Port = 502,
            SlaveId = 1,
            PollInterval = 5000,  // 每5秒轮询一次
            Registers = new List<ModbusRegister>
            {
                new() {
                    Name = "温度",
                    Address = 0,
                    Type = ModbusRegisterType.HoldingRegister,
                    DataType = ModbusDataType.UInt16,
                    Scale = 0.1,
                    Unit = "°C"
                }
            }
        };

        public ModbusPollingService(
            IServiceProvider serviceProvider,
            ILogger<ModbusPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Modbus轮询服务启动");

            // 等待5秒，让其他服务启动完成
            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollDeviceAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "轮询Modbus设备失败");
                }

                await Task.Delay(_device.PollInterval, stoppingToken);
            }
        }

        private async Task PollDeviceAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var modbusService = scope.ServiceProvider.GetRequiredService<IModbusService>();

            var results = await modbusService.ReadAllRegistersAsync(_device);

            foreach (var result in results)
            {
                _logger.LogInformation(
                    "📊 {Device} - {Name}: {Value}{Unit}",
                    _device.Name,
                    result.RegisterName,
                    result.Value,
                    result.Unit);
            }

            // 这里可以将数据发布到MQTT或存储到数据库
        }
    }
}
```

---

## 🌉 Step 6: Modbus到MQTT的桥接

创建 `Services/ModbusMqttBridgeService.cs`：

```csharp
using Day12ModbusAPI.Models;

namespace Day12ModbusAPI.Services
{
    // Modbus → MQTT 桥接服务
    public class ModbusMqttBridgeService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModbusMqttBridgeService> _logger;

        public ModbusMqttBridgeService(
            IServiceProvider serviceProvider,
            ILogger<ModbusMqttBridgeService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🌉 Modbus-MQTT桥接服务启动");

            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await BridgeDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "桥接失败");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task BridgeDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var modbusService = scope.ServiceProvider.GetRequiredService<IModbusService>();
            // var mqttService = scope.ServiceProvider.GetRequiredService<IMqttService>();

            // 1. 从Modbus读取数据
            var device = GetDeviceConfig();
            var results = await modbusService.ReadAllRegistersAsync(device);

            // 2. 转换为MQTT消息格式
            foreach (var result in results)
            {
                var mqttTopic = $"factory/modbus/{device.Id}/{result.RegisterName}";
                var mqttPayload = new
                {
                    deviceId = device.Id,
                    deviceName = device.Name,
                    dataType = result.RegisterName,
                    value = result.Value,
                    unit = result.Unit,
                    timestamp = result.Timestamp
                };

                // 3. 发布到MQTT
                // await mqttService.PublishAsync(mqttTopic, mqttPayload);

                _logger.LogInformation(
                    "📤 Modbus→MQTT: {Topic} = {Value}{Unit}",
                    mqttTopic,
                    result.Value,
                    result.Unit);
            }
        }

        private ModbusDeviceConfig GetDeviceConfig()
        {
            return new ModbusDeviceConfig
            {
                Id = 1,
                Name = "PLC-01",
                IpAddress = "127.0.0.1",
                Port = 502,
                SlaveId = 1,
                Registers = new List<ModbusRegister>
                {
                    new() {
                        Name = "temperature",
                        Address = 0,
                        Type = ModbusRegisterType.HoldingRegister,
                        DataType = ModbusDataType.UInt16,
                        Scale = 0.1,
                        Unit = "°C"
                    }
                }
            };
        }
    }
}
```

---

## ⚙️ Step 7: 配置服务

修改 `Program.cs`：

```csharp
using Day12ModbusAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册Modbus服务
builder.Services.AddScoped<IModbusService, ModbusService>();

// 注册后台轮询服务
builder.Services.AddHostedService<ModbusPollingService>();
// builder.Services.AddHostedService<ModbusMqttBridgeService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 🧪 Step 8: 测试（使用模拟器）

### 启动Modbus模拟器

```bash
# 使用Docker启动Modbus模拟器
docker run -d \
  --name modbus-simulator \
  -p 502:502 \
  oitc/modbus-server

# 或使用pyModSlave
pip install pyModSlave
pyModSlave
```

### 测试API

```bash
# 读取所有寄存器
curl http://localhost:5000/api/modbus/read

# 读取单个寄存器
curl http://localhost:5000/api/modbus/read/温度

# 写入寄存器
curl -X POST http://localhost:5000/api/modbus/write \
  -H "Content-Type: application/json" \
  -d '{"address": 0, "value": 255}'
```

---

## 📝 今日总结

### ✅ 你学会了：
- [x] 使用NModbus库
- [x] 实现Modbus TCP客户端
- [x] 读取和解析寄存器数据
- [x] 不同数据类型的处理
- [x] Modbus轮询服务
- [x] Modbus到MQTT的桥接

### 🔑 核心流程：

```
Modbus设备 → TCP连接 → 发送功能码 → 接收数据 → 解析 → 应用
    ↓
MQTT发布 → Broker → 订阅者 → 监控/存储
```

---

## 🎯 明日预告：Day 13 - MQTT+Modbus整合

明天你将学习：
- 完整的IoT网关实现
- Modbus和MQTT无缝整合
- 设备管理和配置
- 实时监控面板

---

## 💾 作业

1. 实现Modbus设备的自动发现
2. 添加Modbus连接池（复用连接）
3. 实现寄存器配置的动态加载
4. 添加Modbus通信错误重试机制

---

**Modbus客户端完成！明天整合所有功能！🚀**


