using Day10MqttPersistenceAPI.Models;
using Day10MqttPersistenceAPI.Services.Interfaces;


namespace Day10MqttPersistenceAPI.Services.Implementations;

public class DeviceMqttService:IDeviceMqttService
{
    
    private readonly IMqttService _mqttService;
    private readonly ILogger<DeviceMqttService> _logger;
    
    public DeviceMqttService(IMqttService mqttService,ILogger<DeviceMqttService> logger)
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
        var payload = new {deviceId = deviceId, Status = status, Timestamp = DateTime.UtcNow };
        await _mqttService.PublishAsync(topic, payload);
        _logger.LogInformation("发布设备状态: Device={DeviceId}, Topic={Topic}, Status={Status}", deviceId, topic, status);
    }
    
}