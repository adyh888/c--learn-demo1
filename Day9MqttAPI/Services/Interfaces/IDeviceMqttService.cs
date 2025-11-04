using Day9MqttAPI.Models;

namespace Day9MqttAPI.Services.Interfaces;

public interface IDeviceMqttService
{
    Task PublishDeviceDataAsync(int deviceId, DeviceDataMessage data);
    Task PublishDeviceStatusAsync(int deviceId, string status);
}