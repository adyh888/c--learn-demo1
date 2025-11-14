using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;
using Day10MqttPersistenceAPI.Services.Interfaces;
using Day10MqttPersistenceAPI.Models;

namespace Day10MqttPersistenceAPI.Services.Implementations;

public class MqttService:IMqttService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IManagedMqttClient _mqttClient;
    
    private readonly ManagedMqttClientOptions _mqttOptions;
    
    public bool IsConnected => _mqttClient?.IsConnected ?? false;
    
    //增强MQTT服务（自动持久化）
    private readonly IMessagePersistenceService _persistenceService;
    
    public MqttService(ILogger<MqttService> logger,IConfiguration configuration,IMessagePersistenceService persistenceService)
    {
        _logger = logger;
        _persistenceService = persistenceService;
        
       //创建MQTT 客户端
       var factory =  new MqttFactory();
       _mqttClient = factory.CreateManagedMqttClient();
       
       //配置连接选项
       var mqttConfig = configuration.GetSection("Mqtt");
       var port = mqttConfig["Port"];
       var clientOptions =  new MqttClientOptionsBuilder()
           .WithTcpServer(mqttConfig["Server"], port != null ? int.Parse(port) : 1883)
           .WithClientId(mqttConfig["ClientId"] ?? Guid.NewGuid().ToString())
           .WithCleanSession()
           .Build();
        
       _mqttOptions =  new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
           .WithClientOptions(clientOptions).Build();
       
       //设置事件处理
       _mqttClient.ConnectedAsync += OnConnectedAsync;
       _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
       _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;


    }
    
    //启动MQTT客户端
    public async Task StartAsync()
    {
        await _mqttClient.StartAsync(_mqttOptions);
        _logger.LogInformation("MQTT客户端已启动");
    }
    
    //停止MQTT客户端
    public async Task StopAsync()
    {
        await _mqttClient.StopAsync();
        _logger.LogInformation("MQTT客户端已停止");
    }
    
    //发布消息
    public async Task PublishAsync(string topic, object payload)
    {
         var json =  JsonSerializer.Serialize(payload);
         
         var message = new MqttApplicationMessageBuilder()
             .WithTopic(topic)
             .WithPayload(json)
             .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
             .WithRetainFlag(false)
             .Build();
         
         var managedMessage = new ManagedMqttApplicationMessage
         {
             ApplicationMessage = message
         };
         
         await _mqttClient.EnqueueAsync(managedMessage);
         _logger.LogInformation("已发布消息到主题 {Topic} 内容: {Payload}",topic,json);
    }
    
    //订阅主题
    public async Task SubscribeAsync(string topic)
    {
        var topicFilter = new MqttTopicFilterBuilder().WithTopic(topic).Build();
        await _mqttClient.SubscribeAsync(new List<MQTTnet.Packets.MqttTopicFilter> { topicFilter });
        _logger.LogInformation("已订阅主题 {Topic}",topic);
    }
    
    //取消订阅主题
    public async Task UnsubscribeAsync(string topic)
    {
        await _mqttClient.UnsubscribeAsync(new List<string> { topic });
        _logger.LogInformation("已取消订阅主题 {Topic}",topic);
    }
    
    //连接成功事件
    private Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        _logger.LogInformation("MQTT客户端已连接");
        return Task.CompletedTask;
    }
    
    //断开连接事件
    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _logger.LogWarning("MQTT客户端已断开连接，正在尝试重新连接...");
        return Task.CompletedTask;
    }
    
    //收到消息事件
    // private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    // {
    //     var topic = args.ApplicationMessage.Topic;
    //     var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
    //     
    //     _logger.LogInformation("收到消息 主题: {Topic} 消息: {Payload}",topic,payload);
    //     
    //     //这里可以添加处理消息的逻辑
    //     
    //     return Task.CompletedTask;
    // }
    
    
    //增强MQTT服务（自动持久化）
    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
        
        _logger.LogInformation("收到消息 主题: {Topic} 消息: {Payload}",topic,payload);
        
        try
        {
            // 反序列化为 DeviceDataMessage（这是MQTT消息的格式）
            var deviceDataMessage = JsonSerializer.Deserialize<DeviceDataMessage>(payload);
            if (deviceDataMessage != null)
            {
                // 转换为 DeviceMessage（数据库实体）
                var message = new DeviceMessage
                {
                    DeviceId = deviceDataMessage.DeviceId,
                    Topic = topic,
                    DataType = deviceDataMessage.DataType,
                    Value = deviceDataMessage.Value,
                    Unit = deviceDataMessage.Unit,
                    ReceivedAt = DateTime.UtcNow,
                    DeviceTimestamp = deviceDataMessage.Timestamp
                };
                await _persistenceService.SaveMessageAsync(message);
            }
            else
            {
                _logger.LogWarning("无法反序列化消息内容: {Payload}", payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理收到的消息时出错: {Payload}", payload);
        }
    }
    
    
}