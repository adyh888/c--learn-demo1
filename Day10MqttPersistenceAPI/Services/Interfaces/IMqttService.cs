namespace Day10MqttPersistenceAPI.Services.Interfaces;

public interface IMqttService
{
    Task StartAsync();
    
    Task StopAsync();
    
    Task PublishAsync(string topic, object payload);


    Task SubscribeAsync(string topic);
    
    Task UnsubscribeAsync(string topic);
    
    bool IsConnected { get;  }
}