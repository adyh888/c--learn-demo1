using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Day10MqttPersistenceAPI.Services.Interfaces;

//历史数据查询API

namespace Day10MqttPersistenceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceHistoryController : ControllerBase
    {
        private readonly IMessagePersistenceService _messagePersistenceService;

        public DeviceHistoryController(IMessagePersistenceService messagePersistenceService)
        {
            _messagePersistenceService = messagePersistenceService;
        }

        // GET: api/devicehistory/{deviceId}
        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetDeviceHistory(int deviceId, [FromQuery] DateTime? startTime=null, [FromQuery] DateTime? endTime=null, [FromQuery] int limit = 1000)
        {
            var history = await _messagePersistenceService.GetDeviceHistoryAsync(deviceId, startTime, endTime, limit);
            return Ok(new
            {
                deviceId = deviceId,
                count = history.Count,
                data = history
            });
        }
        
        // GET: api/devicehistory/{deviceId}/statistics
        [HttpGet("{deviceId}/statistics")]
        public async Task<IActionResult> GetDeviceStatistics(int deviceId, [FromQuery] string dataType= "temperature", [FromQuery] DateTime? startTime=null, [FromQuery] DateTime? endTime=null)
        {
            var start =  startTime ?? DateTime.UtcNow.AddHours(-24);
            var end = endTime ?? DateTime.UtcNow;
            var stats = await _messagePersistenceService.GetDeviceStatisticsAsync(deviceId, dataType, start, end);
            return Ok(new
            {
                deviceId = deviceId,
                dataType = dataType,
                statistics = stats
            });
        }
        
        // GET: api/devicehistory/{deviceId}/trend
        [HttpGet("{deviceId}/trend")]
        public async Task<IActionResult> GetTrend(int deviceId, [FromQuery] int hours = 24)
        {
            var startTime = DateTime.UtcNow.AddHours(-hours);
            var history = await _messagePersistenceService.GetDeviceHistoryAsync(deviceId, startTime, null,10000);
            
            //按小时分组聚合
            var trend=history.GroupBy(m => new
            {
                Hour = new DateTime(m.DeviceTimestamp.Year, m.DeviceTimestamp.Month, m.DeviceTimestamp.Day, m.DeviceTimestamp.Hour, 0, 0)
            })
            .Select(g => new
            {
                time=g.Key.Hour,
                avgValue=g.Average(m => m.Value),
                minValue=g.Min(m => m.Value),
                maxValue=g.Max(m => m.Value),
                count = g.Count()
            })
            .OrderBy(d => d.time)
            .ToList();
            
            return Ok(new
            {
                deviceId = deviceId,
                hours = hours,
                trend = trend
            });
        }
        
    }
}
