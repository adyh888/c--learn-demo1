# Day 3: 数据模型和内存存储（CRUD完整实现）

> **学习目标**: 设计数据模型、实现完整CRUD、理解对象关系
>
> **预计时间**: 2-3小时
>
> **前置知识**: 完成Day 1-2的学习

---

## 📚 今日知识点

### 核心内容

1. 设计合理的数据模型
2. 模型之间的关系（一对多）
3. 使用静态集合模拟数据库
4. DTO模式的深入应用
5. 数据映射和转换

---

## 🎯 项目目标：设备管理系统

今天我们要构建一个**设备管理系统**，为后面的MQTT/Modbus做准备。

**功能需求**：

- 设备的增删改查
- 设备分类（传感器、控制器、网关等）
- 每个设备有多条历史数据记录
- 设备状态监控（在线/离线）

---

## 🚀 Step 1: 创建项目

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2
dotnet new webapi -n Day3DeviceAPI
cd Day3DeviceAPI
dotnet watch run
```

---

## 📦 Step 2: 设计数据模型

创建 `Models/Device.cs`：

```csharp
namespace Day3DeviceAPI.Models
{
    // 设备类型枚举
    public enum DeviceType
    {
        Sensor = 1,      // 传感器
        Controller = 2,  // 控制器
        Gateway = 3,     // 网关
        Actuator = 4     // 执行器
    }

    // 设备状态枚举
    public enum DeviceStatus
    {
        Offline = 0,     // 离线
        Online = 1,      // 在线
        Error = 2        // 故障
    }

    // 设备模型
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DeviceType Type { get; set; }
        public DeviceStatus Status { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastOnlineAt { get; set; }  // 最后在线时间（可空）

        // 导航属性：一个设备有多条数据记录
        public List<DeviceData> DataRecords { get; set; } = new List<DeviceData>();
    }

    // 设备数据模型
    public class DeviceData
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }      // 外键
        public string DataType { get; set; } = string.Empty;  // 数据类型（温度、湿度等）
        public double Value { get; set; }      // 数据值
        public string Unit { get; set; } = string.Empty;      // 单位
        public DateTime Timestamp { get; set; }

        // 导航属性：数据属于哪个设备
        public Device? Device { get; set; }
    }
}
```

**🔵 与前端TypeScript对比:**

```typescript
// TypeScript 等价定义
enum DeviceType {
  Sensor = 1,
  Controller = 2,
  Gateway = 3,
  Actuator = 4
}

enum DeviceStatus {
  Offline = 0,
  Online = 1,
  Error = 2
}

interface Device {
  id: number;
  name: string;
  description: string;
  type: DeviceType;
  status: DeviceStatus;
  ipAddress: string;
  port: number;
  createdAt: Date;
  lastOnlineAt: Date | null;
  dataRecords: DeviceData[];  // 一对多关系
}

interface DeviceData {
  id: number;
  deviceId: number;           // 外键
  dataType: string;
  value: number;
  unit: string;
  timestamp: Date;
  device?: Device;            // 反向引用
}
```

**📝 关键概念:**

1. **枚举（Enum）**
    - C#的枚举有实际数值（可以序列化为数字）
    - 比字符串更高效，类型更安全

2. **可空类型**
    - `DateTime?` - 可以为null的DateTime
    - `Device?` - 可以为null的Device对象

3. **导航属性**
    - `DataRecords` - 一个设备有多条数据（一对多）
    - `Device` - 每条数据属于一个设备（多对一）
    - 类似SQL的JOIN关系

4. **默认值初始化**
    - `= string.Empty` - 避免null引用
    - `= new List<DeviceData>()` - 初始化空列表

---

## 📝 Step 3: 创建DTO（数据传输对象）

创建 `DTOs/DeviceDtos.cs`：

```csharp
using System.ComponentModel.DataAnnotations;
using Day3DeviceAPI.Models;

namespace Day3DeviceAPI.DTOs
{
    // 创建设备DTO
    public class CreateDeviceDto
    {
        [Required(ErrorMessage = "设备名称不能为空")]
        [MinLength(2, ErrorMessage = "设备名称至少2个字符")]
        [MaxLength(50, ErrorMessage = "设备名称最多50个字符")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "设备类型不能为空")]
        public DeviceType Type { get; set; }

        [Required(ErrorMessage = "IP地址不能为空")]
        [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$", ErrorMessage = "IP地址格式不正确")]
        public string IpAddress { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "端口号必须在1-65535之间")]
        public int Port { get; set; }
    }

    // 更新设备DTO
    public class UpdateDeviceDto
    {
        [MinLength(2)]
        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public DeviceStatus? Status { get; set; }

        [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$")]
        public string? IpAddress { get; set; }

        [Range(1, 65535)]
        public int? Port { get; set; }
    }

    // 设备响应DTO（返回给前端的数据）
    public class DeviceResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;  // 枚举转字符串
        public string Status { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastOnlineAt { get; set; }
        public int DataRecordCount { get; set; }  // 数据记录数量
    }

    // 设备详情DTO（包含数据记录）
    public class DeviceDetailDto : DeviceResponseDto
    {
        public List<DeviceDataDto> RecentData { get; set; } = new List<DeviceDataDto>();
    }

    // 设备数据DTO
    public class DeviceDataDto
    {
        public int Id { get; set; }
        public string DataType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // 创建设备数据DTO
    public class CreateDeviceDataDto
    {
        [Required]
        public string DataType { get; set; } = string.Empty;

        [Required]
        public double Value { get; set; }

        [Required]
        public string Unit { get; set; } = string.Empty;
    }
}
```

**💡 为什么要用DTO？**

| 场景   | 不用DTO的问题               | 使用DTO的好处   |
|------|------------------------|------------|
| 接收数据 | 客户端可以设置ID、CreatedAt等字段 | 只接收必要字段，安全 |
| 返回数据 | 可能暴露敏感信息（密码、内部字段）      | 只返回需要的字段   |
| 验证   | Model混合了业务逻辑和验证逻辑      | 分离关注点，清晰   |
| 版本控制 | API变更会影响数据库模型          | DTO可以独立演化  |

**🔵 前端类比:**

- DTO ≈ 表单数据对象（FormData）
- Model ≈ 数据库实体（Entity）
- ResponseDto ≈ API响应格式（ViewModel）

---

## 🗄️ Step 4: 创建数据存储服务

创建 `Data/DeviceStore.cs`：

```csharp
using Day3DeviceAPI.Models;

namespace Day3DeviceAPI.Data
{
    // 模拟数据库的静态存储
    public static class DeviceStore
    {
        // 设备列表
        public static List<Device> Devices { get; set; } = new List<Device>
        {
            new Device
            {
                Id = 1,
                Name = "温度传感器-01",
                Description = "车间温度监控",
                Type = DeviceType.Sensor,
                Status = DeviceStatus.Online,
                IpAddress = "192.168.1.100",
                Port = 502,
                CreatedAt = DateTime.Now.AddDays(-30),
                LastOnlineAt = DateTime.Now
            },
            new Device
            {
                Id = 2,
                Name = "PLC控制器-01",
                Description = "生产线控制器",
                Type = DeviceType.Controller,
                Status = DeviceStatus.Online,
                IpAddress = "192.168.1.101",
                Port = 502,
                CreatedAt = DateTime.Now.AddDays(-25),
                LastOnlineAt = DateTime.Now.AddMinutes(-5)
            },
            new Device
            {
                Id = 3,
                Name = "MQTT网关-01",
                Description = "物联网网关",
                Type = DeviceType.Gateway,
                Status = DeviceStatus.Offline,
                IpAddress = "192.168.1.102",
                Port = 1883,
                CreatedAt = DateTime.Now.AddDays(-20),
                LastOnlineAt = DateTime.Now.AddHours(-2)
            }
        };

        // 设备数据列表
        public static List<DeviceData> DeviceDataRecords { get; set; } = new List<DeviceData>
        {
            new DeviceData
            {
                Id = 1,
                DeviceId = 1,
                DataType = "温度",
                Value = 25.5,
                Unit = "°C",
                Timestamp = DateTime.Now.AddMinutes(-10)
            },
            new DeviceData
            {
                Id = 2,
                DeviceId = 1,
                DataType = "温度",
                Value = 26.2,
                Unit = "°C",
                Timestamp = DateTime.Now.AddMinutes(-5)
            },
            new DeviceData
            {
                Id = 3,
                DeviceId = 1,
                DataType = "温度",
                Value = 25.8,
                Unit = "°C",
                Timestamp = DateTime.Now
            }
        };

        // 获取下一个设备ID
        public static int GetNextDeviceId()
        {
            return Devices.Any() ? Devices.Max(d => d.Id) + 1 : 1;
        }

        // 获取下一个数据ID
        public static int GetNextDataId()
        {
            return DeviceDataRecords.Any() ? DeviceDataRecords.Max(d => d.Id) + 1 : 1;
        }
    }
}
```

**📝 说明:**

- `static` - 静态类和属性（全局共享，类似单例）
- 在应用运行期间数据会保留
- 重启应用后数据会丢失（明天学数据库持久化）

---

## 🎮 Step 5: 创建设备控制器

创建 `Controllers/DeviceController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day3DeviceAPI.Models;
using Day3DeviceAPI.DTOs;
using Day3DeviceAPI.Data;

namespace Day3DeviceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        // GET: api/device
        [HttpGet]
        public IActionResult GetAllDevices(
            [FromQuery] DeviceType? type = null,
            [FromQuery] DeviceStatus? status = null)
        {
            var devices = DeviceStore.Devices.AsQueryable();

            // 按类型筛选
            if (type.HasValue)
            {
                devices = devices.Where(d => d.Type == type.Value);
            }

            // 按状态筛选
            if (status.HasValue)
            {
                devices = devices.Where(d => d.Status == status.Value);
            }

            // 转换为DTO
            var response = devices.Select(d => new DeviceResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Type = d.Type.ToString(),
                Status = d.Status.ToString(),
                IpAddress = d.IpAddress,
                Port = d.Port,
                CreatedAt = d.CreatedAt,
                LastOnlineAt = d.LastOnlineAt,
                DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == d.Id)
            }).ToList();

            return Ok(response);
        }

        // GET: api/device/{id}
        [HttpGet("{id}")]
        public IActionResult GetDeviceById(int id)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            // 获取最近10条数据
            var recentData = DeviceStore.DeviceDataRecords
                .Where(dd => dd.DeviceId == id)
                .OrderByDescending(dd => dd.Timestamp)
                .Take(10)
                .Select(dd => new DeviceDataDto
                {
                    Id = dd.Id,
                    DataType = dd.DataType,
                    Value = dd.Value,
                    Unit = dd.Unit,
                    Timestamp = dd.Timestamp
                })
                .ToList();

            var response = new DeviceDetailDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Type = device.Type.ToString(),
                Status = device.Status.ToString(),
                IpAddress = device.IpAddress,
                Port = device.Port,
                CreatedAt = device.CreatedAt,
                LastOnlineAt = device.LastOnlineAt,
                DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == id),
                RecentData = recentData
            };

            return Ok(response);
        }

        // POST: api/device
        [HttpPost]
        public IActionResult CreateDevice([FromBody] CreateDeviceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var device = new Device
            {
                Id = DeviceStore.GetNextDeviceId(),
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                Status = DeviceStatus.Offline,  // 新建设备默认离线
                IpAddress = dto.IpAddress,
                Port = dto.Port,
                CreatedAt = DateTime.Now,
                LastOnlineAt = null
            };

            DeviceStore.Devices.Add(device);

            var response = new DeviceResponseDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Type = device.Type.ToString(),
                Status = device.Status.ToString(),
                IpAddress = device.IpAddress,
                Port = device.Port,
                CreatedAt = device.CreatedAt,
                LastOnlineAt = device.LastOnlineAt,
                DataRecordCount = 0
            };

            return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, response);
        }

        // PUT: api/device/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            // 更新字段（只更新提供的字段）
            if (dto.Name != null) device.Name = dto.Name;
            if (dto.Description != null) device.Description = dto.Description;
            if (dto.Status.HasValue) device.Status = dto.Status.Value;
            if (dto.IpAddress != null) device.IpAddress = dto.IpAddress;
            if (dto.Port.HasValue) device.Port = dto.Port.Value;

            var response = new DeviceResponseDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Type = device.Type.ToString(),
                Status = device.Status.ToString(),
                IpAddress = device.IpAddress,
                Port = device.Port,
                CreatedAt = device.CreatedAt,
                LastOnlineAt = device.LastOnlineAt,
                DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == id)
            };

            return Ok(response);
        }

        // DELETE: api/device/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteDevice(int id)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            // 删除设备的所有数据记录
            DeviceStore.DeviceDataRecords.RemoveAll(dd => dd.DeviceId == id);

            // 删除设备
            DeviceStore.Devices.Remove(device);

            return NoContent();
        }

        // POST: api/device/{id}/data
        [HttpPost("{id}/data")]
        public IActionResult AddDeviceData(int id, [FromBody] CreateDeviceDataDto dto)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            var data = new DeviceData
            {
                Id = DeviceStore.GetNextDataId(),
                DeviceId = id,
                DataType = dto.DataType,
                Value = dto.Value,
                Unit = dto.Unit,
                Timestamp = DateTime.Now
            };

            DeviceStore.DeviceDataRecords.Add(data);

            // 更新设备最后在线时间
            device.LastOnlineAt = DateTime.Now;
            device.Status = DeviceStatus.Online;

            return CreatedAtAction(nameof(GetDeviceById), new { id = id }, data);
        }

        // GET: api/device/{id}/data
        [HttpGet("{id}/data")]
        public IActionResult GetDeviceData(
            int id,
            [FromQuery] int limit = 50)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            var data = DeviceStore.DeviceDataRecords
                .Where(dd => dd.DeviceId == id)
                .OrderByDescending(dd => dd.Timestamp)
                .Take(limit)
                .Select(dd => new DeviceDataDto
                {
                    Id = dd.Id,
                    DataType = dd.DataType,
                    Value = dd.Value,
                    Unit = dd.Unit,
                    Timestamp = dd.Timestamp
                })
                .ToList();

            return Ok(data);
        }
    }
}
```

**📝 LINQ方法详解:**

```csharp
// C# LINQ                           // JavaScript 等价
.Where(d => d.Id == id)              // .filter(d => d.id === id)
.Select(d => new DTO { ... })        // .map(d => ({ ... }))
.FirstOrDefault(d => d.Id == id)     // .find(d => d.id === id) || null
.OrderBy(d => d.CreatedAt)           // .sort((a,b) => a.createdAt - b.createdAt)
.OrderByDescending(d => d.Timestamp) // .sort((a,b) => b.timestamp - a.timestamp)
.Take(10)                            // .slice(0, 10)
.Count(d => d.Status == Online)      // .filter(d => d.status === 'online').length
.Any(d => d.Id == id)                // .some(d => d.id === id)
.AsQueryable()                       // 转换为可查询对象（链式调用）
```

---

## 🧪 Step 6: 测试API

### 测试脚本（可以保存为 test.sh）

```bash
#!/bin/bash
API="http://localhost:5000/api/device"

echo "1. 获取所有设备"
curl -s $API | json_pp

echo "\n2. 获取设备详情"
curl -s $API/1 | json_pp

echo "\n3. 创建新设备"
curl -s -X POST $API \
  -H "Content-Type: application/json" \
  -d '{
    "name": "压力传感器-01",
    "description": "管道压力监控",
    "type": 1,
    "ipAddress": "192.168.1.103",
    "port": 502
  }' | json_pp

echo "\n4. 筛选传感器类型设备"
curl -s "$API?type=1" | json_pp

echo "\n5. 添加设备数据"
curl -s -X POST $API/1/data \
  -H "Content-Type: application/json" \
  -d '{
    "dataType": "温度",
    "value": 27.3,
    "unit": "°C"
  }' | json_pp

echo "\n6. 获取设备数据"
curl -s $API/1/data | json_pp
```

---

## 📝 今日总结

### ✅ 你学会了：

- [x] 设计合理的数据模型（Model）
- [x] 使用枚举（Enum）
- [x] DTO模式的应用
- [x] 一对多关系的表示
- [x] 静态存储模拟数据库
- [x] LINQ查询和数据转换
- [x] 完整的CRUD操作

### 🔑 关键概念对比：

| C#概念      | 前端对应               | 说明     |
|-----------|--------------------|--------|
| Model     | Entity/Interface   | 数据结构   |
| DTO       | FormData/ViewModel | 数据传输对象 |
| Enum      | Enum/Union Type    | 枚举类型   |
| LINQ      | Array Methods      | 数据查询   |
| `static`  | Global/Singleton   | 静态/全局  |
| `List<T>` | `Array<T>`         | 列表/数组  |

---

## 🎯 明日预告：Day 4 - Entity Framework Core + SQLite

明天你将学习：

- 使用真实数据库（SQLite）
- Entity Framework Core（ORM）
- 数据库迁移（Migrations）
- DbContext的使用
- 异步数据库操作

---

## 💾 作业

1. 添加设备统计API：
   ```csharp
   GET /api/device/statistics
   // 返回：总设备数、在线设备数、离线设备数、各类型设备数
   ```

2. 添加设备搜索：
   ```csharp
   GET /api/device/search?keyword=温度
   // 按名称或描述搜索
   ```

3. 添加批量操作：
   ```csharp
   PUT /api/device/batch/status
   // 批量修改设备状态
   ```

4. 思考：
    - 为什么要分离Model和DTO？
    - 如果不用LINQ，代码会变成什么样？
    - 一对多关系在前端怎么展示？

---

**明天我们就要用真实数据库了！🎉**


