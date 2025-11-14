using Day10MqttPersistenceAPI.Models;
using Day10MqttPersistenceAPI.DTOs;
using Day10MqttPersistenceAPI.Services.Interfaces;
using Day10MqttPersistenceAPI.Repositories.Interfaces;
using Day10MqttPersistenceAPI.Middleware;


namespace Day10MqttPersistenceAPI.Services.Implementations;

public class DeviceService:IDevicesService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<DeviceService> _logger;
    
    //依赖注入:注入Repository和Logger
    public DeviceService(IDeviceRepository deviceRepository,ILogger<DeviceService> logger)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
    }


    public async Task<List<DeviceResponseDto>> GetAllDevicesAsync(DeviceType? type = null, DeviceStatus? status = null)
    {
        // var devices = await _deviceRepository.GetAllAsync();

        List<Device> devices;
        //根据type和status过滤
        if (type.HasValue && status.HasValue )
        {
            devices = await _deviceRepository.GetByTypeAsync(type.Value);
            devices = devices.Where(d => d.Status == status.Value).ToList();
        } else if (type.HasValue)
        {
            devices = await _deviceRepository.GetByTypeAsync(type.Value);
        }else if (status.HasValue)
        {
            devices=await _deviceRepository.GetByStatusAsync(status.Value);
            
        }
        else
        {
            devices = await _deviceRepository.GetAllAsync();
        }
        
        //转换为DTO
        return devices.Select(MapToResponseDto).ToList();

        
        // //映射到DTO
        // var deviceDtos = devices.Select(d => new DeviceResponseDto
        // {
        //     Id = d.Id,
        //     Name = d.Name,
        //     Type = d.Type,
        //     Status = d.Status,
        //     IpAddress = d.IpAddress
        // }).ToList();
        //
        // return deviceDtos;
    }

    public async Task<DeviceDetailDto?> GetDeviceByIdAsync(int id)
    {
        var device = await _deviceRepository.GetByIdWithDataAsync(id);
        if (device == null)
        {
            throw new NotFoundException($"设备ID {id} 不存在");
        }

        //映射到详细DTO
        var deviceDetailDto = new DeviceDetailDto
        {
            Id = device.Id,
            Name = device.Name,
            Description = device.Description,
            Type = device.Type.ToString(),
            Status = device.Status.ToString(),
            IpAddress = device.IpAddress,
            Port=device.Port,
            CreatedAt=device.CreatedAt,
            LastOnlineAt=device.LastOnlineAt,
            RecentData = device.DataRecords.Select(dr => new DeviceDataDto
            {
                Id = dr.Id,
                Timestamp = dr.Timestamp,
                Value = dr.Value,
                DataType = dr.DataType,
                Unit = dr.Unit
            }).ToList()
        };

        return deviceDetailDto;
    }
    
    
    
    public async Task<DeviceResponseDto> CreateDeviceAsync(CreateDeviceDto dto)
    {
        //业务验证:检查IP是否已存在
        if(await _deviceRepository.ExistsByIpAsync(dto.IpAddress))
        {
            _logger.LogWarning($"尝试创建重复IP的设备: {dto.IpAddress}");
            throw new InvalidOperationException($"IP地址 {dto.IpAddress} 已被使用");
        }
        
        //创建设备对象
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
        
        //保存到数据库
        var createdDevice = await _deviceRepository.CreateAsync(device);
        
        _logger.LogInformation($"创建设备成功: {device.Id} - {device.Name}");
        
        return MapToResponseDto(createdDevice);
        
    }
    
    
    public async Task<DeviceResponseDto?> UpdateDeviceAsync(int id, UpdateDeviceDto dto)
    {
        var existingDevice = await _deviceRepository.GetByIdAsync(id);
        if (existingDevice == null)
        {
            throw new NotFoundException($"设备ID {id} 不存在");
        }
        
        // 如果更新IP，检查是否重复
        if (dto.IpAddress != null && dto.IpAddress != existingDevice.IpAddress)
        {
            if (await _deviceRepository.ExistsByIpAsync(dto.IpAddress))
            {
                throw new ValidationException($"IP地址 {dto.IpAddress} 已被使用", new Dictionary<string, string[]>());
            }
        }
        
        //更新字段
        existingDevice.Name = dto.Name ?? existingDevice.Name;
        existingDevice.Description = dto.Description ?? existingDevice.Description;
        existingDevice.IpAddress = dto.IpAddress ?? existingDevice.IpAddress;
        existingDevice.Port = dto.Port ?? existingDevice.Port;
        
        var updatedDevice = await _deviceRepository.UpdateAsync(existingDevice);
        
        _logger.LogInformation($"更新设备成功: {updatedDevice.Id} - {updatedDevice.Name}");
        
        return MapToResponseDto(updatedDevice);
    }
    
    
    public async Task<bool> DeleteDeviceAsync(int id)
    {
        var existingDevice = await _deviceRepository.GetByIdAsync(id);
        if (existingDevice == null)
        {
            return false;
        }
        
        await _deviceRepository.DeleteAsync(existingDevice);
        
        _logger.LogInformation($"删除设备成功: {existingDevice.Id} - {existingDevice.Name}");
        
        return true;
    }
    
    //业务方法
    public async Task<bool> SetDeviceOnlineAsync(int id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null)
        {
            throw new NotFoundException($"设备ID {id} 不存在");
        }
        
        device.Status = DeviceStatus.Online;
        device.LastOnlineAt = DateTime.Now;
        
        await _deviceRepository.UpdateAsync(device);
        
        _logger.LogInformation($"设备设为在线: {device.Id} - {device.Name}");
        
        return true;
    }
    
    public async Task<bool> SetDeviceOfflineAsync(int id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null)
        {
            return false;
        }
        
        device.Status = DeviceStatus.Offline;
        
        await _deviceRepository.UpdateAsync(device);
        
        _logger.LogInformation($"设备设为离线: {device.Id} - {device.Name}");
        
        return true;
    }
    
    public async Task<DeviceStatistcsDto> GetDeviceStatisticsAsync()
    {
        var total = await _deviceRepository.CountAsync();
        var online = await _deviceRepository.CountByStatusAsync(DeviceStatus.Online);
        var offline = await _deviceRepository.CountByStatusAsync(DeviceStatus.Offline);
        var errorDevices = await _deviceRepository.CountByStatusAsync(DeviceStatus.Error);
        
        var allDevices = await _deviceRepository.GetAllAsync();
        var devicesByType = allDevices
            .GroupBy(d => d.Type)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());
        
        return new DeviceStatistcsDto
        {
            TotalDevices = total,
            OnlineDevices = online,
            OfflineDevices = offline,
            ErrorDevices = errorDevices,
            DevicesByType = devicesByType
        };
    }
    
    
    //私有辅助方法:Model转DTO
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