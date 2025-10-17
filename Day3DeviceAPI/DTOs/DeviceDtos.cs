using System.ComponentModel.DataAnnotations;
using Day3DeviceAPI.Models;

namespace Day3DeviceAPI.DTOs;

public class DeviceDtos
{

}

//创建设备DTO
public class CreateDeviceDto
{
    [Required(ErrorMessage = "设备名称不能为空")]
    [MinLength(2, ErrorMessage = "设备名称至少包含2个字符")]
    [MaxLength(50, ErrorMessage = "设备名称不能超过50个字符")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "设备描述不能超过200个字符")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "设备类型不能为空")]
    public DeviceType Type { get; set; }

    // [Required]
    // public DeviceStatus Status { get; set; }

    [Required(ErrorMessage = "IP地址不能为空")]
    [RegularExpression(@"^(25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)){3}$", ErrorMessage = "无效的IP地址格式")]
    public string IpAddress { get; set; } = string.Empty;

    [Range(1,65535,ErrorMessage = "端口号必须在1到65535之间")]
    public int Port { get; set; }
}

//更新设备DTO
public class UpdateDeviceDto
{
    [MinLength(2, ErrorMessage = "设备名称至少包含2个字符")]
    [MaxLength(50, ErrorMessage = "设备名称不能超过50个字符")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "设备描述不能超过200个字符")]
    public string Description { get; set; } = string.Empty;

    public DeviceStatus Status { get; set; }

    [RegularExpression(@"^(25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)){3}$", ErrorMessage = "无效的IP地址格式")]
    public string IpAddress { get; set; } = string.Empty;
    
    [Range(1,65535,ErrorMessage = "端口号必须在1到65535之间")]
    public int Port { get; set; }
}

//设备响应DTO(返回给前端的数据)
public class DeviceResponseDto
{
    public int Id { get; set; } //设备ID
    public string Name { get; set; } = string.Empty; //设备名称
    public string Description { get; set; } = string.Empty; //设备描述
    public string Type { get; set; }=string.Empty; 
    public string Status { get; set; }=string.Empty;
    public string IpAddress { get; set; } = string.Empty; //设备IP地址
    public int Port { get; set; } //设备端口
    public DateTime CreatedAt { get; set; } //创建时间
    public DateTime? LastOnlineAt { get; set; } //最后上线时间(可空)

    public int DataRecordCount { get; set; } //数据记录数量
}

//批量更新设备状态DTO
public class BatchUpdateDeviceStatusDto
{
    [Required(ErrorMessage = "设备ID列表不能为空")]
    public List<int> DeviceIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "设备状态不能为空")]
    public DeviceStatus Status { get; set; }
}


//设备详情DTO(包含数据记录)
public class DeviceDetailDto:DeviceResponseDto
{
    public List<DeviceDataDto> RecentData { get; set; } = new List<DeviceDataDto>();
}

//设备数据的DTO
public class DeviceDataDto
{
    public int Id { get; set; } //数据记录ID
    public string DataType { get; set; } = string.Empty; //数据类型(如温度,湿度等)
    public double Value { get; set; } //数据值(存储为字符串,可根据需要转换)
    public string Unit { get; set; } = string.Empty; //数据单位(如摄氏度,百分比等)
    public DateTime Timestamp { get; set; } //数据时间戳
}

//创建设备数据DTO
public class CreateDeviceDataDto
{
    [Required(ErrorMessage = "数据类型不能为空")]
    public string DataType { get; set; } = string.Empty;

    [Required(ErrorMessage = "数据值不能为空")]
    public double Value { get; set; }

    [MaxLength(20, ErrorMessage = "数据单位不能超过20个字符")]
    public string Unit { get; set; } = string.Empty;
}