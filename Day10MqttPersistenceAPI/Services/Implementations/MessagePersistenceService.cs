using Day10MqttPersistenceAPI.Data;
using Day10MqttPersistenceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Day10MqttPersistenceAPI.Services.Interfaces;

namespace Day10MqttPersistenceAPI.Services.Implementations;

//消息持久化服务
public class MessagePersistenceService:IMessagePersistenceService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessagePersistenceService> _logger;
    
    public MessagePersistenceService(IServiceProvider serviceProvider,ILogger<MessagePersistenceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    
    public async Task SaveMessageAsync(DeviceMessage message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            message.ReceivedAt = DateTime.UtcNow;
            await dbContext.DeviceMessages.AddAsync(message);
            await dbContext.SaveChangesAsync();
            
            _logger.LogDebug("保存消息: Device={DeviceId}, Type={DataType}, Value={Value}",
                message.DeviceId, message.DataType, message.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 保存消息失败: Device={DeviceId}, Type={DataType}, Value={Value}",
                message.DeviceId, message.DataType, message.Value);
        }
    }
    
    
    public async Task<List<DeviceMessage>> GetDeviceHistoryAsync(int deviceId,DateTime? startTime,DateTime? endTime,int limt =1000)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var query = dbContext.DeviceMessages
            .Where(m => m.DeviceId == deviceId)
            .AsQueryable();
        
        if (startTime.HasValue)
        {
            query = query.Where(m => m.DeviceTimestamp >= startTime.Value);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(m => m.DeviceTimestamp <= endTime.Value);
        }
        
        return await query
            .OrderByDescending(m => m.DeviceTimestamp)
            .Take(limt)
            .ToListAsync();
    }
     
    public async Task<DeviceStatistics> GetDeviceStatisticsAsync(int deviceId, string dataType, DateTime startTime, DateTime endTime)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var dataPoints = await dbContext.DeviceMessages
            .Where(m => m.DeviceId == deviceId &&
                        m.DataType == dataType &&
                        m.DeviceTimestamp >= startTime &&
                        m.DeviceTimestamp <= endTime)
            .ToListAsync();
        
        if (!dataPoints.Any())
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
        
        var values = dataPoints.Select(m => m.Value).ToList();

        return new DeviceStatistics
        {
            DeviceId = deviceId,
            DataType = dataType,
            PeriodStart = startTime,
            PeriodEnd = endTime,
            MinValue = dataPoints.Min(m => m.Value),
            MaxValue = dataPoints.Max(m => m.Value),
            AvgValue = dataPoints.Average(m => m.Value),
            Count = dataPoints.Count
        };
    }
    
    
}