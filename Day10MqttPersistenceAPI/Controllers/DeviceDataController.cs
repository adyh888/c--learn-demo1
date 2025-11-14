using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Day10MqttPersistenceAPI.Models;
using Day10MqttPersistenceAPI.Services.Interfaces;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceDataController : ControllerBase
    {
        private readonly IDeviceMqttService _deviceMqttService;
        
        public DeviceDataController(IDeviceMqttService deviceMqttService)
        {
            _deviceMqttService = deviceMqttService;
        }
        
        //POST: api/devicedata/report
        [HttpPost("report")]
        public async Task<IActionResult> ReportData([FromBody] DeviceDataMessage dataMessage)
        {
            dataMessage.Timestamp = DateTime.UtcNow;
            await _deviceMqttService.PublishDeviceDataAsync(dataMessage.DeviceId, dataMessage);
            return Ok(new { Message = "设备数据已发布到MQTT代理。",data = dataMessage });
        }
        
        //POST: api/devicedata/status/
        [HttpPost("{deviceId}/status")]
        public async Task<IActionResult> UpdateStatus(int deviceId, [FromBody] StatusUpdate statusUpdate)
        {
            await _deviceMqttService.PublishDeviceStatusAsync(deviceId, statusUpdate.Status);
            return Ok(new { Message = "设备状态已发布到MQTT代理。", DeviceId = deviceId, Status = statusUpdate.Status });
        }
        
        
        
    }
    
    public class StatusUpdate
    {
        public string Status { get; set; } = string.Empty;
    }
}
