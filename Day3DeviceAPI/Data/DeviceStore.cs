using Day3DeviceAPI.Models;

namespace Day3DeviceAPI.Data;

//模拟数据库的静态存储
public static class DeviceStore
{
    //设备列表
    public static List<Device> Devices { get; set; } = new List<Device>
    {
        new Device
        {
            Id = 1,
            Name = "温度传感器-01",
            Description = "车间温度监控",
            Type = DeviceType.Sensor,
            Status = DeviceStatus.Online,
            IpAddress = "192.168.1.100",
            Port = 502,
            CreatedAt = DateTime.Now.AddDays(-30),
            LastOnlineAt = DateTime.Now
        },
        new Device
        {
            Id = 2,
            Name = "PLC控制器-01",
            Description = "生产线控制器",
            Type = DeviceType.Controller,
            Status = DeviceStatus.Online,
            IpAddress = "192.168.1.101",
            Port = 502,
            CreatedAt = DateTime.Now.AddDays(-25),
            LastOnlineAt = DateTime.Now.AddMinutes(-5)
        },
        new Device
        {
            Id = 3,
            Name = "MQTT网关-01",
            Description = "物联网网关",
            Type = DeviceType.Gateway,
            Status = DeviceStatus.Offline,
            IpAddress = "192.168.1.102",
            Port = 1883,
            CreatedAt = DateTime.Now.AddDays(-20),
            LastOnlineAt = DateTime.Now.AddHours(-2)
        },
        new Device
        {
        Id = 4,
        Name = "MQTT网关-02",
        Description = "物联网网关",
        Type = DeviceType.Gateway,
        Status = DeviceStatus.Offline,
        IpAddress = "192.168.1.102",
        Port = 1883,
        CreatedAt = DateTime.Now.AddDays(-20),
        LastOnlineAt = DateTime.Now.AddHours(-2)
    }
    };
        
        //设备数据列表
        public static List<DeviceData> DeviceDataRecords { get; set; } = new List<DeviceData>
        {
            new DeviceData
            {
                Id = 1,
                DeviceId = 1,
                DataType = "温度",
                Value = 25.5,
                Unit = "°C",
                Timestamp = DateTime.Now.AddMinutes(-10)
            },
            new DeviceData
            {
                Id = 2,
                DeviceId = 1,
                DataType = "温度",
                Value = 26.2,
                Unit = "°C",
                Timestamp = DateTime.Now.AddMinutes(-5)
            },
            new DeviceData
            {
                Id = 3,
                DeviceId = 4,
                DataType = "温度",
                Value = 25.8,
                Unit = "°C",
                Timestamp = DateTime.Now
            }
        };
    
            //获取下一个设备ID
    public static int GetNextDeviceId()
            {
                return Devices.Any() ? Devices.Max(d => d.Id) + 1 : 1;
            }
            
            //获取下一个数据ID
    public static int GetNextDataId()
    {
        return DeviceDataRecords.Any() ? DeviceDataRecords.Max(d => d.Id) + 1 : 1;
    }
            
}