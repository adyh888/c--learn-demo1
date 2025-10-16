# Day 10: MQTT消息持久化与数据聚合

> **学习目标**: 将MQTT消息存储到数据库，实现历史数据查询和统计
> 
> **预计时间**: 2-3小时

---

## 🎯 今日目标

将MQTT接收到的设备数据自动存储到数据库，并提供：
- 历史数据查询
- 数据聚合统计
- 实时 + 历史数据结合展示

---

## 📦 Step 1: 扩展数据模型

创建 `Models/DeviceMessage.cs`：

```csharp
using System.ComponentModel.DataAnnotations;

namespace Day10MqttPersistenceAPI.Models
{
    // MQTT消息记录（存储所有收到的消息）
    public class DeviceMessage
    {
        [Key]
        public long Id { get; set; }
        
        public int DeviceId { get; set; }
        
        public string Topic { get; set; } = string.Empty;
        
        public string DataType { get; set; } = string.Empty;
        
        public double Value { get; set; }
        
        public string Unit { get; set; } = string.Empty;
        
        public DateTime ReceivedAt { get; set; }
        
        public DateTime DeviceTimestamp { get; set; }
    }

    // 设备统计（聚合数据）
    public class DeviceStatistics
    {
        [Key]
        public long Id { get; set; }
        
        public int DeviceId { get; set; }
        
        public string DataType { get; set; } = string.Empty;
        
        public DateTime PeriodStart { get; set; }
        
        public DateTime PeriodEnd { get; set; }
        
        public double MinValue { get; set; }
        
        public double MaxValue { get; set; }
        
        public double AvgValue { get; set; }
        
        public int Count { get; set; }
    }
}
```

---

## 🗄️ Step 2: 更新DbContext

创建/更新 `Data/AppDbContext.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Day10MqttPersistenceAPI.Models;

namespace Day10MqttPersistenceAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DeviceMessage> DeviceMessages { get; set; }
        public DbSet<DeviceStatistics> DeviceStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 索引优化
            modelBuilder.Entity<DeviceMessage>(entity =>
            {
                entity.HasIndex(e => e.DeviceId);
                entity.HasIndex(e => e.DeviceTimestamp);
                entity.HasIndex(e => new { e.DeviceId, e.DataType, e.DeviceTimestamp });
            });

            modelBuilder.Entity<DeviceStatistics>(entity =>
            {
                entity.HasIndex(e => e.DeviceId);
                entity.HasIndex(e => new { e.DeviceId, e.PeriodStart });
            });
        }
    }
}
```

**运行迁移：**
```bash
dotnet ef migrations add AddMqttPersistence
dotnet ef database update
```

---

## 💾 Step 3: 创建消息持久化服务

创建 `Services/MessagePersistenceService.cs`：

```csharp
using Day10MqttPersistenceAPI.Data;
using Day10MqttPersistenceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Day10MqttPersistenceAPI.Services
{
    public interface IMessagePersistenceService
    {
        Task SaveMessageAsync(DeviceMessage message);
        Task<List<DeviceMessage>> GetDeviceHistoryAsync(int deviceId, DateTime? startTime, DateTime? endTime, int limit = 1000);
        Task<DeviceStatistics> GetDeviceStatisticsAsync(int deviceId, string dataType, DateTime startTime, DateTime endTime);
    }

    public class MessagePersistenceService : IMessagePersistenceService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessagePersistenceService> _logger;

        public MessagePersistenceService(
            IServiceProvider serviceProvider,
            ILogger<MessagePersistenceService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SaveMessageAsync(DeviceMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            message.ReceivedAt = DateTime.UtcNow;
            context.DeviceMessages.Add(message);
            await context.SaveChangesAsync();

            _logger.LogDebug("保存消息: Device={DeviceId}, Type={DataType}, Value={Value}",
                message.DeviceId, message.DataType, message.Value);
        }

        public async Task<List<DeviceMessage>> GetDeviceHistoryAsync(
            int deviceId,
            DateTime? startTime,
            DateTime? endTime,
            int limit = 1000)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = context.DeviceMessages
                .Where(m => m.DeviceId == deviceId)
                .AsQueryable();

            if (startTime.HasValue)
                query = query.Where(m => m.DeviceTimestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(m => m.DeviceTimestamp <= endTime.Value);

            return await query
                .OrderByDescending(m => m.DeviceTimestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<DeviceStatistics> GetDeviceStatisticsAsync(
            int deviceId,
            string dataType,
            DateTime startTime,
            DateTime endTime)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var messages = await context.DeviceMessages
                .Where(m => m.DeviceId == deviceId
                    && m.DataType == dataType
                    && m.DeviceTimestamp >= startTime
                    && m.DeviceTimestamp <= endTime)
                .ToListAsync();

            if (!messages.Any())
            {
                return new DeviceStatistics
                {
                    DeviceId = deviceId,
                    DataType = dataType,
                    PeriodStart = startTime,
                    PeriodEnd = endTime,
                    Count = 0
                };
            }

            return new DeviceStatistics
            {
                DeviceId = deviceId,
                DataType = dataType,
                PeriodStart = startTime,
                PeriodEnd = endTime,
                MinValue = messages.Min(m => m.Value),
                MaxValue = messages.Max(m => m.Value),
                AvgValue = messages.Average(m => m.Value),
                Count = messages.Count
            };
        }
    }
}
```

---

## 📨 Step 4: 增强MQTT服务（自动持久化）

修改 `Services/MqttService.cs`，添加消息持久化：

```csharp
private readonly IMessagePersistenceService _persistenceService;

public MqttService(
    ILogger<MqttService> logger,
    IConfiguration configuration,
    IMessagePersistenceService persistenceService)  // 新增
{
    _logger = logger;
    _persistenceService = persistenceService;
    // ... 其他初始化代码
}

private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
{
    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
    _logger.LogInformation("📨 收到消息 - 主题: {Topic}, 负载: {Payload}",
        e.ApplicationMessage.Topic, payload);

    try
    {
        // 解析消息并保存到数据库
        var data = JsonSerializer.Deserialize<DeviceDataMessage>(payload);
        
        if (data != null && e.ApplicationMessage.Topic.Contains("/data"))
        {
            var message = new DeviceMessage
            {
                DeviceId = data.DeviceId,
                Topic = e.ApplicationMessage.Topic,
                DataType = data.DataType,
                Value = data.Value,
                Unit = data.Unit,
                DeviceTimestamp = data.Timestamp
            };

            await _persistenceService.SaveMessageAsync(message);
            _logger.LogInformation("✅ 消息已保存到数据库");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "保存MQTT消息失败");
    }
}
```

---

## 🎮 Step 5: 创建历史数据查询API

创建 `Controllers/DeviceHistoryController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Day10MqttPersistenceAPI.Services;

namespace Day10MqttPersistenceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceHistoryController : ControllerBase
    {
        private readonly IMessagePersistenceService _persistenceService;

        public DeviceHistoryController(IMessagePersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        // GET: api/devicehistory/{deviceId}
        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetHistory(
            int deviceId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] int limit = 1000)
        {
            var history = await _persistenceService.GetDeviceHistoryAsync(
                deviceId, startTime, endTime, limit);

            return Ok(new
            {
                deviceId = deviceId,
                count = history.Count,
                data = history
            });
        }

        // GET: api/devicehistory/{deviceId}/statistics
        [HttpGet("{deviceId}/statistics")]
        public async Task<IActionResult> GetStatistics(
            int deviceId,
            [FromQuery] string dataType = "temperature",
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            var start = startTime ?? DateTime.UtcNow.AddHours(-24);
            var end = endTime ?? DateTime.UtcNow;

            var stats = await _persistenceService.GetDeviceStatisticsAsync(
                deviceId, dataType, start, end);

            return Ok(stats);
        }

        // GET: api/devicehistory/{deviceId}/trend
        [HttpGet("{deviceId}/trend")]
        public async Task<IActionResult> GetTrend(
            int deviceId,
            [FromQuery] int hours = 24)
        {
            var startTime = DateTime.UtcNow.AddHours(-hours);
            var history = await _persistenceService.GetDeviceHistoryAsync(
                deviceId, startTime, null, 10000);

            // 按小时分组聚合
            var trend = history
                .GroupBy(m => new
                {
                    Hour = new DateTime(
                        m.DeviceTimestamp.Year,
                        m.DeviceTimestamp.Month,
                        m.DeviceTimestamp.Day,
                        m.DeviceTimestamp.Hour,
                        0, 0)
                })
                .Select(g => new
                {
                    time = g.Key.Hour,
                    avgValue = g.Average(m => m.Value),
                    minValue = g.Min(m => m.Value),
                    maxValue = g.Max(m => m.Value),
                    count = g.Count()
                })
                .OrderBy(t => t.time)
                .ToList();

            return Ok(new
            {
                deviceId = deviceId,
                hours = hours,
                trend = trend
            });
        }
    }
}
```

---

## 📊 Step 6: 定时数据聚合服务

创建 `Services/DataAggregationService.cs`：

```csharp
using Day10MqttPersistenceAPI.Data;
using Day10MqttPersistenceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Day10MqttPersistenceAPI.Services
{
    // 后台服务：定时聚合历史数据
    public class DataAggregationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataAggregationService> _logger;

        public DataAggregationService(
            IServiceProvider serviceProvider,
            ILogger<DataAggregationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("数据聚合服务启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AggregateHourlyDataAsync();
                    await CleanOldDataAsync();  // 清理旧数据
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "数据聚合失败");
                }

                // 每小时执行一次
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task AggregateHourlyDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var hourStart = new DateTime(
                oneHourAgo.Year,
                oneHourAgo.Month,
                oneHourAgo.Day,
                oneHourAgo.Hour,
                0, 0);
            var hourEnd = hourStart.AddHours(1);

            // 获取该小时的所有设备和数据类型
            var groups = await context.DeviceMessages
                .Where(m => m.DeviceTimestamp >= hourStart && m.DeviceTimestamp < hourEnd)
                .GroupBy(m => new { m.DeviceId, m.DataType })
                .ToListAsync();

            foreach (var group in groups)
            {
                var stats = new DeviceStatistics
                {
                    DeviceId = group.Key.DeviceId,
                    DataType = group.Key.DataType,
                    PeriodStart = hourStart,
                    PeriodEnd = hourEnd,
                    MinValue = group.Min(m => m.Value),
                    MaxValue = group.Max(m => m.Value),
                    AvgValue = group.Average(m => m.Value),
                    Count = group.Count()
                };

                // 检查是否已存在
                var existing = await context.DeviceStatistics
                    .FirstOrDefaultAsync(s =>
                        s.DeviceId == stats.DeviceId &&
                        s.DataType == stats.DataType &&
                        s.PeriodStart == stats.PeriodStart);

                if (existing == null)
                {
                    context.DeviceStatistics.Add(stats);
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("✅ 完成数据聚合: {HourStart}", hourStart);
        }

        private async Task CleanOldDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 删除30天前的原始数据
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var oldMessages = await context.DeviceMessages
                .Where(m => m.DeviceTimestamp < cutoffDate)
                .ToListAsync();

            if (oldMessages.Any())
            {
                context.DeviceMessages.RemoveRange(oldMessages);
                await context.SaveChangesAsync();
                _logger.LogInformation("🗑️ 清理了 {Count} 条旧数据", oldMessages.Count);
            }
        }
    }
}
```

---

## ⚙️ Step 7: 注册所有服务

修改 `Program.cs`：

```csharp
using Day10MqttPersistenceAPI.Data;
using Day10MqttPersistenceAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 数据库
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// MQTT服务
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<IMessagePersistenceService, MessagePersistenceService>();

// 后台服务
builder.Services.AddHostedService<DeviceSimulatorService>();
builder.Services.AddHostedService<DataAggregationService>();

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

// 启动MQTT
var mqttService = app.Services.GetRequiredService<IMqttService>();
await mqttService.StartAsync();
await mqttService.SubscribeAsync("factory/device/+/data");

app.Run();
```

---

## 🧪 Step 8: 测试完整流程

### 1. 查看实时数据（MQTT）
```bash
# 监控MQTT日志，看到消息持续到达
```

### 2. 查询历史数据
```bash
# 获取设备1的历史数据
curl "http://localhost:5000/api/devicehistory/1?limit=10"

# 获取最近24小时的数据
curl "http://localhost:5000/api/devicehistory/1?startTime=2025-10-14T00:00:00Z&endTime=2025-10-15T00:00:00Z"
```

### 3. 查询统计数据
```bash
# 获取设备1的温度统计
curl "http://localhost:5000/api/devicehistory/1/statistics?dataType=temperature&hours=24"
```

### 4. 查询趋势数据
```bash
# 获取最近24小时的趋势
curl "http://localhost:5000/api/devicehistory/1/trend?hours=24"
```

---

## 📝 今日总结

### ✅ 你学会了：
- [x] MQTT消息持久化到数据库
- [x] 历史数据查询和筛选
- [x] 数据聚合和统计
- [x] 定时后台任务
- [x] 数据清理策略
- [x] 趋势分析API

### 🔑 数据流程：

```
MQTT消息 → MqttService收到 → 解析JSON → 保存到数据库
    ↓                              ↓
实时展示                        历史查询
    ↓                              ↓
前端监控页面                   统计分析图表
```

---

## 🎯 明日预告：Day 11 - Modbus协议入门

明天开始学习工业设备通信：
- Modbus协议原理
- Modbus TCP/RTU
- 寄存器读写
- 数据解析

---

## 💾 作业

1. 实现数据导出功能（CSV/Excel）
2. 添加数据异常检测（温度过高报警）
3. 实现数据压缩存储（减少数据库大小）
4. 优化查询性能（添加更多索引）

---

**MQTT数据链路打通！明天进入Modbus世界！🚀**


