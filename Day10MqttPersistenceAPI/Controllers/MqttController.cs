using Day10MqttPersistenceAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace Day10MqttPersistenceAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MqttController:ControllerBase
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<MqttController> _logger;
    
    public MqttController(IMqttService mqttService,ILogger<MqttController> logger)
    {
        _mqttService = mqttService;
        _logger = logger;
    }
    
    //GET:api/mqtt/status
    [HttpGet("status")]
    public IActionResult GetMqttStatus()
    {
        return Ok(new
        {
            isConnected = _mqttService.IsConnected,
            timestamp = DateTime.Now
        });
    }
    
    //POST:api/mqtt/publish
    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request)
    {
       if(!_mqttService.IsConnected)
       {
           return BadRequest(new { error = "MQTT客户端未连接" });
       }
       await _mqttService.PublishAsync(request.Topic, request.Payload);

       return Ok(new
       {
           message = "消息已发布",
           topic = request.Topic,
           timestamp = DateTime.Now
       });
    }
    
    
    //POST:api/mqtt/subscribe
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        if(!_mqttService.IsConnected)
        {
            return BadRequest(new { error = "MQTT客户端未连接" });
        }
        
        await _mqttService.SubscribeAsync(request.Topic);
        
        return Ok(new
        {
            message = "已订阅主题",
            topic = request.Topic,
            timestamp = DateTime.Now
        });
    }
    
    
    //POST:api/mqtt/unsubscribe
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] SubscribeRequest request)
    {
        
        await _mqttService.UnsubscribeAsync(request.Topic);
        
        return Ok(new
        {
            message = "已取消订阅主题",
            topic = request.Topic,
            timestamp = DateTime.Now
        });
    }
    
    
    
    // 请求模型
    public class PublishRequest
    {
        public string Topic { get; set; } = string.Empty;
        public object Payload { get; set; } = new { };
    }

    public class SubscribeRequest
    {
        public string Topic { get; set; } = string.Empty;
    }
}