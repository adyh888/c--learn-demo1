namespace Day9MqttAPI.Models;

public class DeviceDataMessage
{
    public int DeviceId { get; set; }
    
    public string DataType { get; set; } = string.Empty;
    
    public double Value { get; set; }
    
    public string Unit { get; set; }=string.Empty;
    
    public DateTime Timestamp { get; set; }=DateTime.Now;
}