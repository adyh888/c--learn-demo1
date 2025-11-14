using Day10MqttPersistenceAPI.Models;

namespace Day10MqttPersistenceAPI.Services.Interfaces;

public interface IMessagePersistenceService
{
    Task SaveMessageAsync(DeviceMessage message);
    
    Task<List<DeviceMessage>> GetDeviceHistoryAsync(int deviceId,DateTime? startTime,DateTime? endTime,int limt =1000);
    
    Task<DeviceStatistics> GetDeviceStatisticsAsync(int deviceId, string dataType, DateTime startTime, DateTime endTime);
}