using System.ComponentModel.DataAnnotations;

namespace Day10MqttPersistenceAPI.Models;

// MQTT消息记录（存储所有收到的消息）
public class DeviceMessage
{
    [Key]
    public long Id { get; set; }
    
    public int DeviceId { get; set; }
    
    public string Topic { get; set; } =string.Empty;
    
    public string DataType { get; set; } = string.Empty;
    
    public double Value { get; set; }
    
    public string Unit { get; set; } = string.Empty;
    
    public DateTime ReceivedAt { get; set;  }
    
    public DateTime DeviceTimestamp { get; set;  }
}

//设备统计(聚合数据)
public class DeviceStatistics
{
    [Key] public long Id { get; set; }
    public int DeviceId { get; set; }

    public string DataType { get; set; } = string.Empty;
    
    public DateTime PeriodStart { get; set;  }
    
    public DateTime PeriodEnd { get; set;  }
    
    public double MinValue { get; set; }
    
    public double MaxValue { get; set; }
    
    public double AvgValue { get; set; }
    
    public int Count { get; set;  }

}
