# Day 5: 服务层架构和依赖注入

> **学习目标**: 理解分层架构、掌握依赖注入、实现业务逻辑层
> 
> **预计时间**: 2-3小时
> 
> **前置知识**: 完成Day 1-4的学习

---

## 📚 今日知识点

### 核心内容
1. 什么是依赖注入（DI）
2. 为什么需要服务层
3. Repository模式
4. 单元测试友好的架构
5. ASP.NET Core的DI容器

---

## 🎯 为什么需要服务层？

### 问题：Controller太臃肿

```csharp
// ❌ 不好的做法：业务逻辑在Controller中
[HttpPost]
public async Task<IActionResult> CreateDevice([FromBody] Device device)
{
    // 验证IP地址
    if (!IsValidIpAddress(device.IpAddress)) 
        return BadRequest("IP地址无效");
    
    // 检查IP是否已存在
    if (await _context.Devices.AnyAsync(d => d.IpAddress == device.IpAddress))
        return BadRequest("IP地址已存在");
    
    // 检查端口范围
    if (device.Port < 1 || device.Port > 65535)
        return BadRequest("端口号无效");
    
    // 创建设备
    device.CreatedAt = DateTime.Now;
    _context.Devices.Add(device);
    await _context.SaveChangesAsync();
    
    // 发送通知
    await SendNotificationAsync($"新设备: {device.Name}");
    
    // 记录日志
    _logger.LogInformation($"Created device: {device.Id}");
    
    return Ok(device);
}
```

**问题**:
- Controller职责过多（验证、业务逻辑、数据访问、日志、通知...）
- 代码难以测试
- 代码难以复用
- 违反单一职责原则

---

### 解决方案：分层架构

```
┌─────────────────────────────────┐
│   Controller Layer (控制器层)    │  ← 处理HTTP请求/响应
│   只负责：接收请求、返回响应      │
└─────────────────────────────────┘
            ↓
┌─────────────────────────────────┐
│   Service Layer (服务层/业务层)  │  ← 业务逻辑
│   只负责：业务规则、验证、协调    │
└─────────────────────────────────┘
            ↓
┌─────────────────────────────────┐
│   Repository Layer (仓储层)      │  ← 数据访问
│   只负责：与数据库交互            │
└─────────────────────────────────┘
            ↓
┌─────────────────────────────────┐
│   Database (数据库)              │
└─────────────────────────────────┘
```

**🔵 前端类比:**
```
React组件 (Component)          ← Controller
    ↓
自定义Hooks / Services         ← Service Layer
    ↓
API调用 / Data Fetching        ← Repository
    ↓
后端API                        ← Database
```

---

## 🚀 Step 1: 创建项目结构

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2
dotnet new webapi -n Day5ServiceLayerAPI
cd Day5ServiceLayerAPI

# 项目结构
# Day5ServiceLayerAPI/
# ├── Controllers/
# ├── Services/           # 新增：业务逻辑层
# │   ├── Interfaces/     # 接口定义
# │   └── Implementations/ # 实现
# ├── Repositories/       # 新增：数据访问层
# │   ├── Interfaces/
# │   └── Implementations/
# ├── Models/
# ├── DTOs/
# └── Data/
```

---

## 📦 Step 2: 创建Repository层

### 2.1 创建Repository接口

创建 `Repositories/Interfaces/IDeviceRepository.cs`：

```csharp
using Day5ServiceLayerAPI.Models;

namespace Day5ServiceLayerAPI.Repositories.Interfaces
{
    // Repository接口：定义数据访问方法
    public interface IDeviceRepository
    {
        // 查询
        Task<List<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(int id);
        Task<Device?> GetByIdWithDataAsync(int id);
        Task<List<Device>> GetByTypeAsync(DeviceType type);
        Task<List<Device>> GetByStatusAsync(DeviceStatus status);
        Task<bool> ExistsByIpAsync(string ipAddress);
        
        // 创建
        Task<Device> CreateAsync(Device device);
        
        // 更新
        Task<Device> UpdateAsync(Device device);
        
        // 删除
        Task DeleteAsync(Device device);
        
        // 统计
        Task<int> CountAsync();
        Task<int> CountByStatusAsync(DeviceStatus status);
    }
}
```

**🔵 前端对比:**
```typescript
// TypeScript接口
interface DeviceRepository {
  getAllAsync(): Promise<Device[]>;
  getByIdAsync(id: number): Promise<Device | null>;
  createAsync(device: Device): Promise<Device>;
  updateAsync(device: Device): Promise<Device>;
  deleteAsync(device: Device): Promise<void>;
}
```

### 2.2 实现Repository

创建 `Repositories/Implementations/DeviceRepository.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Day5ServiceLayerAPI.Data;
using Day5ServiceLayerAPI.Models;
using Day5ServiceLayerAPI.Repositories.Interfaces;

namespace Day5ServiceLayerAPI.Repositories.Implementations
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly AppDbContext _context;

        public DeviceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Device>> GetAllAsync()
        {
            return await _context.Devices.ToListAsync();
        }

        public async Task<Device?> GetByIdAsync(int id)
        {
            return await _context.Devices.FindAsync(id);
        }

        public async Task<Device?> GetByIdWithDataAsync(int id)
        {
            return await _context.Devices
                .Include(d => d.DataRecords.OrderByDescending(dd => dd.Timestamp).Take(10))
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Device>> GetByTypeAsync(DeviceType type)
        {
            return await _context.Devices
                .Where(d => d.Type == type)
                .ToListAsync();
        }

        public async Task<List<Device>> GetByStatusAsync(DeviceStatus status)
        {
            return await _context.Devices
                .Where(d => d.Status == status)
                .ToListAsync();
        }

        public async Task<bool> ExistsByIpAsync(string ipAddress)
        {
            return await _context.Devices
                .AnyAsync(d => d.IpAddress == ipAddress);
        }

        public async Task<Device> CreateAsync(Device device)
        {
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task<Device> UpdateAsync(Device device)
        {
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();
            return device;
        }

        public async Task DeleteAsync(Device device)
        {
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Devices.CountAsync();
        }

        public async Task<int> CountByStatusAsync(DeviceStatus status)
        {
            return await _context.Devices.CountAsync(d => d.Status == status);
        }
    }
}
```

**📝 Repository模式的好处:**
- 封装数据访问逻辑
- 易于测试（可以mock）
- 可以切换数据源（从SQLite换成PostgreSQL，只需改Repository实现）
- 避免在Service中直接操作DbContext

---

## 🎯 Step 3: 创建Service层

### 3.1 创建Service接口

创建 `Services/Interfaces/IDeviceService.cs`：

```csharp
using Day5ServiceLayerAPI.Models;
using Day5ServiceLayerAPI.DTOs;

namespace Day5ServiceLayerAPI.Services.Interfaces
{
    public interface IDeviceService
    {
        // 查询
        Task<List<DeviceResponseDto>> GetAllDevicesAsync(DeviceType? type = null, DeviceStatus? status = null);
        Task<DeviceDetailDto?> GetDeviceByIdAsync(int id);
        
        // 创建
        Task<DeviceResponseDto> CreateDeviceAsync(CreateDeviceDto dto);
        
        // 更新
        Task<DeviceResponseDto?> UpdateDeviceAsync(int id, UpdateDeviceDto dto);
        
        // 删除
        Task<bool> DeleteDeviceAsync(int id);
        
        // 业务方法
        Task<bool> SetDeviceOnlineAsync(int id);
        Task<bool> SetDeviceOfflineAsync(int id);
        Task<DeviceStatisticsDto> GetStatisticsAsync();
    }
}
```

### 3.2 创建DTO

创建 `DTOs/DeviceDtos.cs`：

```csharp
using System.ComponentModel.DataAnnotations;
using Day5ServiceLayerAPI.Models;

namespace Day5ServiceLayerAPI.DTOs
{
    public class CreateDeviceDto
    {
        [Required(ErrorMessage = "设备名称不能为空")]
        [MinLength(2), MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DeviceType Type { get; set; }

        [Required]
        [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$", ErrorMessage = "IP地址格式不正确")]
        public string IpAddress { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; }
    }

    public class UpdateDeviceDto
    {
        [MinLength(2), MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$")]
        public string? IpAddress { get; set; }

        [Range(1, 65535)]
        public int? Port { get; set; }
    }

    public class DeviceResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastOnlineAt { get; set; }
    }

    public class DeviceDetailDto : DeviceResponseDto
    {
        public List<DeviceDataDto> RecentData { get; set; } = new List<DeviceDataDto>();
    }

    public class DeviceDataDto
    {
        public int Id { get; set; }
        public string DataType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class DeviceStatisticsDto
    {
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
        public int ErrorDevices { get; set; }
        public Dictionary<string, int> DevicesByType { get; set; } = new Dictionary<string, int>();
    }
}
```

### 3.3 实现Service

创建 `Services/Implementations/DeviceService.cs`：

```csharp
using Day5ServiceLayerAPI.Models;
using Day5ServiceLayerAPI.DTOs;
using Day5ServiceLayerAPI.Services.Interfaces;
using Day5ServiceLayerAPI.Repositories.Interfaces;

namespace Day5ServiceLayerAPI.Services.Implementations
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<DeviceService> _logger;

        // 依赖注入：注入Repository和Logger
        public DeviceService(
            IDeviceRepository deviceRepository,
            ILogger<DeviceService> logger)
        {
            _deviceRepository = deviceRepository;
            _logger = logger;
        }

        public async Task<List<DeviceResponseDto>> GetAllDevicesAsync(
            DeviceType? type = null, 
            DeviceStatus? status = null)
        {
            List<Device> devices;

            // 根据筛选条件查询
            if (type.HasValue && status.HasValue)
            {
                devices = await _deviceRepository.GetByTypeAsync(type.Value);
                devices = devices.Where(d => d.Status == status.Value).ToList();
            }
            else if (type.HasValue)
            {
                devices = await _deviceRepository.GetByTypeAsync(type.Value);
            }
            else if (status.HasValue)
            {
                devices = await _deviceRepository.GetByStatusAsync(status.Value);
            }
            else
            {
                devices = await _deviceRepository.GetAllAsync();
            }

            // 转换为DTO
            return devices.Select(MapToResponseDto).ToList();
        }

        public async Task<DeviceDetailDto?> GetDeviceByIdAsync(int id)
        {
            var device = await _deviceRepository.GetByIdWithDataAsync(id);

            if (device == null)
                return null;

            return new DeviceDetailDto
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
                RecentData = device.DataRecords.Select(d => new DeviceDataDto
                {
                    Id = d.Id,
                    DataType = d.DataType,
                    Value = d.Value,
                    Unit = d.Unit,
                    Timestamp = d.Timestamp
                }).ToList()
            };
        }

        public async Task<DeviceResponseDto> CreateDeviceAsync(CreateDeviceDto dto)
        {
            // 业务验证：检查IP是否已存在
            if (await _deviceRepository.ExistsByIpAsync(dto.IpAddress))
            {
                _logger.LogWarning($"尝试创建重复IP的设备: {dto.IpAddress}");
                throw new InvalidOperationException($"IP地址 {dto.IpAddress} 已被使用");
            }

            // 创建设备对象
            var device = new Device
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                Status = DeviceStatus.Offline,
                IpAddress = dto.IpAddress,
                Port = dto.Port,
                CreatedAt = DateTime.Now,
                LastOnlineAt = null
            };

            // 保存到数据库
            device = await _deviceRepository.CreateAsync(device);

            _logger.LogInformation($"创建设备成功: {device.Id} - {device.Name}");

            return MapToResponseDto(device);
        }

        public async Task<DeviceResponseDto?> UpdateDeviceAsync(int id, UpdateDeviceDto dto)
        {
            var device = await _deviceRepository.GetByIdAsync(id);

            if (device == null)
                return null;

            // 如果更新IP，检查是否重复
            if (dto.IpAddress != null && dto.IpAddress != device.IpAddress)
            {
                if (await _deviceRepository.ExistsByIpAsync(dto.IpAddress))
                {
                    throw new InvalidOperationException($"IP地址 {dto.IpAddress} 已被使用");
                }
            }

            // 更新字段
            if (dto.Name != null) device.Name = dto.Name;
            if (dto.Description != null) device.Description = dto.Description;
            if (dto.IpAddress != null) device.IpAddress = dto.IpAddress;
            if (dto.Port.HasValue) device.Port = dto.Port.Value;

            device = await _deviceRepository.UpdateAsync(device);

            _logger.LogInformation($"更新设备成功: {device.Id} - {device.Name}");

            return MapToResponseDto(device);
        }

        public async Task<bool> DeleteDeviceAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);

            if (device == null)
                return false;

            await _deviceRepository.DeleteAsync(device);

            _logger.LogInformation($"删除设备成功: {id}");

            return true;
        }

        public async Task<bool> SetDeviceOnlineAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);

            if (device == null)
                return false;

            device.Status = DeviceStatus.Online;
            device.LastOnlineAt = DateTime.Now;

            await _deviceRepository.UpdateAsync(device);

            _logger.LogInformation($"设备上线: {id} - {device.Name}");

            return true;
        }

        public async Task<bool> SetDeviceOfflineAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);

            if (device == null)
                return false;

            device.Status = DeviceStatus.Offline;

            await _deviceRepository.UpdateAsync(device);

            _logger.LogInformation($"设备离线: {id} - {device.Name}");

            return true;
        }

        public async Task<DeviceStatisticsDto> GetStatisticsAsync()
        {
            var totalDevices = await _deviceRepository.CountAsync();
            var onlineDevices = await _deviceRepository.CountByStatusAsync(DeviceStatus.Online);
            var offlineDevices = await _deviceRepository.CountByStatusAsync(DeviceStatus.Offline);
            var errorDevices = await _deviceRepository.CountByStatusAsync(DeviceStatus.Error);

            var allDevices = await _deviceRepository.GetAllAsync();
            var devicesByType = allDevices
                .GroupBy(d => d.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return new DeviceStatisticsDto
            {
                TotalDevices = totalDevices,
                OnlineDevices = onlineDevices,
                OfflineDevices = offlineDevices,
                ErrorDevices = errorDevices,
                DevicesByType = devicesByType
            };
        }

        // 私有辅助方法：Model转DTO
        private DeviceResponseDto MapToResponseDto(Device device)
        {
            return new DeviceResponseDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Type = device.Type.ToString(),
                Status = device.Status.ToString(),
                IpAddress = device.IpAddress,
                Port = device.Port,
                CreatedAt = device.CreatedAt,
                LastOnlineAt = device.LastOnlineAt
            };
        }
    }
}
```

**📝 Service层的职责:**
- ✅ 业务逻辑（验证IP重复、更新设备状态等）
- ✅ 协调多个Repository
- ✅ 数据转换（Model ↔ DTO）
- ✅ 日志记录
- ✅ 异常处理
- ❌ 不直接操作数据库（交给Repository）
- ❌ 不处理HTTP细节（交给Controller）

---

## 🎮 Step 4: 简化Controller

创建 `Controllers/DeviceController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day5ServiceLayerAPI.DTOs;
using Day5ServiceLayerAPI.Models;
using Day5ServiceLayerAPI.Services.Interfaces;

namespace Day5ServiceLayerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        // 依赖注入：注入Service（不是Repository）
        public DeviceController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        // GET: api/device
        [HttpGet]
        public async Task<IActionResult> GetAllDevices(
            [FromQuery] DeviceType? type = null,
            [FromQuery] DeviceStatus? status = null)
        {
            var devices = await _deviceService.GetAllDevicesAsync(type, status);
            return Ok(devices);
        }

        // GET: api/device/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(int id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);

            if (device == null)
                return NotFound(new { error = $"设备ID {id} 不存在" });

            return Ok(device);
        }

        // POST: api/device
        [HttpPost]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var device = await _deviceService.CreateDeviceAsync(dto);
                return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, device);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: api/device/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var device = await _deviceService.UpdateDeviceAsync(id, dto);

                if (device == null)
                    return NotFound(new { error = $"设备ID {id} 不存在" });

                return Ok(device);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // DELETE: api/device/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var success = await _deviceService.DeleteDeviceAsync(id);

            if (!success)
                return NotFound(new { error = $"设备ID {id} 不存在" });

            return NoContent();
        }

        // PUT: api/device/{id}/online
        [HttpPut("{id}/online")]
        public async Task<IActionResult> SetDeviceOnline(int id)
        {
            var success = await _deviceService.SetDeviceOnlineAsync(id);

            if (!success)
                return NotFound(new { error = $"设备ID {id} 不存在" });

            return Ok(new { message = "设备已上线" });
        }

        // PUT: api/device/{id}/offline
        [HttpPut("{id}/offline")]
        public async Task<IActionResult> SetDeviceOffline(int id)
        {
            var success = await _deviceService.SetDeviceOfflineAsync(id);

            if (!success)
                return NotFound(new { error = $"设备ID {id} 不存在" });

            return Ok(new { message = "设备已离线" });
        }

        // GET: api/device/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var statistics = await _deviceService.GetStatisticsAsync();
            return Ok(statistics);
        }
    }
}
```

**对比：Controller变得非常简洁！**
- 只负责接收请求、调用Service、返回响应
- 没有业务逻辑
- 易于测试

---

## ⚙️ Step 5: 配置依赖注入

修改 `Program.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Day5ServiceLayerAPI.Data;
using Day5ServiceLayerAPI.Repositories.Interfaces;
using Day5ServiceLayerAPI.Repositories.Implementations;
using Day5ServiceLayerAPI.Services.Interfaces;
using Day5ServiceLayerAPI.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// 注册DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册Repository（Scoped生命周期）
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// 注册Service（Scoped生命周期）
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

**📝 DI生命周期:**

| 生命周期 | 说明 | 使用场景 |
|---------|------|----------|
| **Transient** | 每次请求都创建新实例 | 轻量级、无状态的服务 |
| **Scoped** | 每个HTTP请求创建一个实例 | Repository、DbContext |
| **Singleton** | 整个应用只有一个实例 | 配置、缓存 |

**🔵 前端类比:**
```typescript
// React Context/Provider
<DeviceServiceProvider>  {/* Singleton/Scoped */}
  <App />
</DeviceServiceProvider>
```

---

## 📝 今日总结

### ✅ 你学会了：
- [x] 依赖注入（DI）的原理和使用
- [x] Repository模式
- [x] Service层设计
- [x] 分层架构的优势
- [x] ASP.NET Core的DI容器
- [x] 接口驱动开发

### 🔑 架构对比：

**没有分层（Day 4）:**
```
Controller → DbContext → Database
(所有逻辑混在一起)
```

**分层架构（Day 5）:**
```
Controller → Service → Repository → DbContext → Database
(职责清晰，易于维护和测试)
```

**🔵 前端类比:**
```
React Component → Custom Hook → API Service → Backend
```

---

## 🎯 明日预告：Day 6 - 异步编程和LINQ高级

明天你将学习：
- async/await深入理解
- LINQ高级查询
- 性能优化
- 并发处理

---

## 💾 作业

1. 理解依赖注入的三种生命周期
2. 尝试为DeviceData也创建Repository和Service
3. 思考：如果要切换到MySQL，需要改哪些代码？
4. 思考：如何对Service进行单元测试？

---

**架构升级完成！明天继续深入！🚀**


