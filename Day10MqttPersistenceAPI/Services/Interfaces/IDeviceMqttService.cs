using Day10MqttPersistenceAPI.Models;

namespace Day10MqttPersistenceAPI.Services.Interfaces;

public interface IDeviceMqttService
{
    Task PublishDeviceDataAsync(int deviceId, DeviceDataMessage data);
    Task PublishDeviceStatusAsync(int deviceId, string status);
}