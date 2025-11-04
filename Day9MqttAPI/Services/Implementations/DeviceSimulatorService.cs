using Day9MqttAPI.Services.Interfaces;
using Day9MqttAPI.Models;


namespace Day9MqttAPI.Services.Implementations;

public class DeviceSimulatorService:BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceSimulatorService> _logger;
    
    public DeviceSimulatorService(IServiceProvider serviceProvider,ILogger<DeviceSimulatorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("设备模拟器服务已启动");
        
        // 等待5秒让MQTT连接完成
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var deviceMqtt = scope.ServiceProvider.GetRequiredService<IDeviceMqttService>();

                // 模拟3个设备上报数据
                for (int deviceId = 1; deviceId <= 3; deviceId++)
                {
                    var data = new Models.DeviceDataMessage
                    {
                        DeviceId = deviceId,
                        DataType = "temperature",
                        Value = 20 + Random.Shared.NextDouble() * 10,  // 20-30°C
                        Unit = "°C",
                        Timestamp = DateTime.UtcNow
                    };

                    await deviceMqtt.PublishDeviceDataAsync(deviceId, data);
                }

                _logger.LogInformation("✅ 模拟设备数据已发布");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布模拟数据失败");
            }

            // 每10秒发布一次
            await Task.Delay(60000, stoppingToken);
        }
    }
    
}