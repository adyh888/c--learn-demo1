using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Day9MqttAPI.Models;

public enum DeviceType
{
    Sensor = 1,
    Actuator =4,
    Controller =2,
    Gateway =3
}

public enum DeviceStatus
{
    Online = 1,
    Offline = 0,
    Error = 2
}


public class Device
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] //自增
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }  = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; }  = string.Empty;
    
    [Required]
    public DeviceType Type { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;

    [Required]
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    
    [Range(1, 65535)]
    public int Port { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? LastOnlineAt { get; set; }
    
    //导航属性:一对多
    public virtual ICollection<DeviceData> DataRecords { get; set; }=new List<DeviceData>();


}


public class DeviceData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] //自增
    public int Id { get; set; }

    [Required]
    public int DeviceId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty;
    
    [Required]
    public double Value { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    //导航属性:多对一
    [ForeignKey("DeviceId")]
    [JsonIgnore]
    public virtual Device? Device { get; set; }
    
    
}

