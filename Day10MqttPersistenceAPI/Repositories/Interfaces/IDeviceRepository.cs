using Day10MqttPersistenceAPI.Models;

namespace Day10MqttPersistenceAPI.Repositories.Interfaces;

// Repository接口：定义数据访问方法
public interface IDeviceRepository
{
    //查询
    Task<List<Device>> GetAllAsync();
    
    Task<Device?> GetByIdAsync(int id);
    
    Task<Device?> GetByIdWithDataAsync(int id);
    
    Task<List<Device>> GetByTypeAsync(DeviceType type);
    
    
    Task<List<Device>> GetByStatusAsync(DeviceStatus status);
    
    Task<bool>ExistsByIpAsync(string ipAddress);
    
    
    //创建
    Task<Device> CreateAsync(Device device);
    
    //更新
    Task<Device> UpdateAsync(Device device);
    
    //删除
    Task DeleteAsync(Device device);
    
    //统计
    Task<int> CountAsync();
    Task<int> CountByStatusAsync(DeviceStatus status);

}