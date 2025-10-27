using System.ComponentModel.DataAnnotations;
using Day7MiddlewareAPI.Models;


namespace Day7MiddlewareAPI.DTOs;

public class DeviceDtos
{
    
}


public class CreateDeviceDto
{
    [Required(ErrorMessage = "Device name is required.")]
    [MinLength(2),MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Device type is required.")]
    public DeviceType Type { get; set; }
    
    [Required(ErrorMessage = "IP address is required.")]
    [RegularExpression(@"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "Invalid IP address format.")]
    public string IpAddress { get; set; } = string.Empty;
    
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; }
    
    
}


public class UpdateDeviceDto
{
    
    [MinLength(2),MaxLength(50)]
    public string? Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Description { get; set; } = string.Empty;
    
    
    [RegularExpression(@"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "Invalid IP address format.")]
    public string? IpAddress { get; set; } = string.Empty;
    
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int? Port { get; set; }
    
}


public class DeviceResponseDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    
    public string IpAddress { get; set; } = string.Empty;
    
    public int Port { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastOnlineAt { get; set; }
}

public class DeviceDetailDto : DeviceResponseDto
{
    public List<DeviceDataDto> RecentData { get; set; } = new List<DeviceDataDto>();
}

public class DeviceDataDto
{
    public int Id { get; set; }
    
    public string DataType { get; set; } = string.Empty;
    
    public double Value { get; set; }
    
    
    public string Unit { get; set; }=string.Empty;
    public DateTime Timestamp { get; set; }
}

public class DeviceStatistcsDto
{
    public int TotalDevices { get; set; }
    
    public int OnlineDevices { get; set; }
    
    public int OfflineDevices { get; set; }
    
    public int ErrorDevices { get; set; }
    
    public Dictionary<string, int> DevicesByType { get; set; } = new Dictionary<string, int>();
}