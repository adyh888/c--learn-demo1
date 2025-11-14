using Day10MqttPersistenceAPI.Data;
using Day10MqttPersistenceAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace Day10MqttPersistenceAPI.Services.Implementations;

//定时数据聚合服务
public class DataAggregationService:BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataAggregationService> _logger;
    
    public DataAggregationService(IServiceProvider serviceProvider,ILogger<DataAggregationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 数据聚合服务已启动");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AggregateHourlyDataAsync();
                await CleanOldDataAsync();  // 清理旧数据
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 数据聚合失败");
            }
            
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
        
        _logger.LogInformation("🛑 数据聚合服务已停止");
    }
    
    private async Task AggregateHourlyDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var startOfHour = new DateTime(oneHourAgo.Year, oneHourAgo.Month, oneHourAgo.Day, oneHourAgo.Hour, 0, 0, DateTimeKind.Utc);
        var endOfHour = startOfHour.AddHours(1);
        
        // 获取该小时的所有设备和数据类型
        var statistics = await dbContext.DeviceMessages.Where(m =>m.DeviceTimestamp >= startOfHour && m.DeviceTimestamp < endOfHour)
            .GroupBy(m => new {m.DeviceId, m.DataType})
            .ToListAsync();

        foreach (var group in statistics)
        {
            var stats = new DeviceStatistics
            {
                DeviceId = group.Key.DeviceId,
                DataType = group.Key.DataType,
                PeriodStart = startOfHour,
                PeriodEnd = endOfHour,
                MinValue = group.Min(m => m.Value),
                MaxValue = group.Max(m => m.Value),
                AvgValue = group.Average(m => m.Value),
                Count = group.Count()
            };
            
            //检查是否已经存在
            var existing = await dbContext.DeviceStatistics.FirstOrDefaultAsync(s =>
                s.DeviceId == stats.DeviceId &&
                s.DataType == stats.DataType &&
                s.PeriodStart == stats.PeriodStart);

            if (existing == null)
            {
                dbContext.DeviceStatistics.Add(stats);
            }
        }
        
        await dbContext.SaveChangesAsync();
        
        _logger.LogInformation("✅ 已聚合小时数据: {Count} 条记录", statistics.Count);
    }

    private async Task CleanOldDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        //删除30天前的原始数据
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldMessages = await dbContext.DeviceMessages.Where(m => m.DeviceTimestamp < cutoffDate).ToListAsync();
        
        if(oldMessages.Any())
        {
            dbContext.DeviceMessages.RemoveRange(oldMessages);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("🧹 已删除 {Count} 条过期原始数据", oldMessages.Count);
        }

    }
    
    
}