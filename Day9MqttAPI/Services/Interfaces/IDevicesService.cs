using Day9MqttAPI.Models;
using Day9MqttAPI.DTOs;

namespace Day9MqttAPI.Services.Interfaces;

public interface IDevicesService
{
    //查询
    Task<List<DeviceResponseDto>> GetAllDevicesAsync(DeviceType? type = null, DeviceStatus? status = null);
    
    Task<DeviceDetailDto?> GetDeviceByIdAsync(int id);
    
    //创建
    Task<DeviceResponseDto> CreateDeviceAsync(CreateDeviceDto createDeviceDto);
    
    //更新
    Task<DeviceResponseDto?> UpdateDeviceAsync(int id, UpdateDeviceDto updateDeviceDto);
    
    //删除
    Task<bool> DeleteDeviceAsync(int id);
    
    //业务方法
    Task<bool> SetDeviceOnlineAsync(int id);
    Task<bool> SetDeviceOfflineAsync(int id);
    
    Task<DeviceStatistcsDto> GetDeviceStatisticsAsync();
    
}