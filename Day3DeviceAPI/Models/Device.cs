namespace Day3DeviceAPI.Models;

//设备类型枚举
public enum DeviceType
{
    Sensor = 1 , //传感器
    Controller = 2 , //控制器
    Gateway = 3, //网关
    Actuator = 4 //执行器
}
    
//设备状态枚举
public enum DeviceStatus
{
    Online = 1 , //在线
    Offline =0  , //离线
    Error = 2 , //故障
    Maintenance = 3 //维护中
}

//设备模型
public class Device
{
    public int Id { get; set; } //设备ID
    public string Name { get; set; } = string.Empty; //设备名称

    public string Description { get; set; } = string.Empty; //设备描述
    
    public DeviceType Type { get; set; } //设备类型
    public DeviceStatus Status { get; set; } //设备状态
    public string IpAddress { get; set; } = string.Empty; //设备IP地址
    public int Port { get; set; } //设备端口
    
    public DateTime CreatedAt { get; set; } //创建时间
    
    public DateTime? LastOnlineAt { get; set; } //最后上线时间(可空)
    
    //导航属性:一个设备有多条数据记录
    public List<DeviceData> DataRecords { get; set; } = new List<DeviceData>();
   
    
    
}

//设备数据模型
public class DeviceData
{
    public int Id { get; set; } //数据记录ID
    public int DeviceId { get; set; } //关联设备ID

    public string DataType { get; set; } = string.Empty; //数据类型(如温度,湿度等)
    public double Value { get; set; }  //数据值(存储为字符串,可根据需要转换)

    public string Unit { get; set; } = string.Empty; //数据单位(如摄氏度,百分比等)
    public DateTime Timestamp { get; set; } //数据时间戳
    
    //导航属性:数据记录所属设备
    public Device? Device { get; set; } =new Device();
}