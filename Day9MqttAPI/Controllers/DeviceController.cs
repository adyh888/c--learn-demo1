using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Day9MqttAPI.DTOs;
using Day9MqttAPI.Models;
using Day9MqttAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly IDevicesService _deviceService;
        
        // private readonly AppDbContext _context;
        
        //通过依赖注入获取DbContext
        // public DeviceController(AppDbContext context)
        // {
        //     _context = context;
        // }
        
        //依赖注入：注入Service
        public DeviceController(IDevicesService deviceService)
        {
           _deviceService = deviceService;
        }
        
        
        // GET: api/Device
        [HttpGet]
        public async Task<ActionResult> GetAllDevices(
            [FromQuery] DeviceType? type =null,
            [FromQuery] DeviceStatus? status =null
            )
        {
            var devices = await _deviceService.GetAllDevicesAsync(type, status);
            return Ok(devices);
        }
        
        //GET: api/Device/5
        [HttpGet("{id}")]
        public async Task<ActionResult> GetDeviceById(int id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            return Ok(device);
        }
        
        
        //POST: api/Device
        [HttpPost]
        public async Task<ActionResult> CreateDevice([FromBody] CreateDeviceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var device = await _deviceService.CreateDeviceAsync(dto);
                return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, device);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        
        
        //PUT: api/Device/5
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
        {
            // 不需要try-catch，异常会被中间件处理
            var device = await _deviceService.UpdateDeviceAsync(id, dto);
            return Ok(device);
        }
        
        
        //DELETE: api/Device/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDevice(int id)
        {
            var success = await _deviceService.DeleteDeviceAsync(id);
            if(!success)
                return NotFound(new { error = $"设备ID {id} 不存在" });
            
            return NoContent();
        }
        
        // PUT: api/device/{id}/online
        [HttpPut("{id}/online")]
        public async Task<ActionResult> SetDeviceOnline(int id)
        {
            var success = await _deviceService.SetDeviceOnlineAsync(id);
            return Ok(new { message = "设备已上线" });
        }
        
        // PUT: api/device/{id}/offline
        [HttpPut("{id}/offline")]
        public async Task<ActionResult> SetDeviceOffline(int id)
        {
            var success = await _deviceService.SetDeviceOfflineAsync(id);
            if (!success)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            return Ok(new { message = "设备已离线" });
        }
        
        // GET: api/device/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult> GetDeviceStatistics()
        {
            var stats = await _deviceService.GetDeviceStatisticsAsync();
            return Ok(stats);
        }
        
        
        
        // //POST: api/device/{id}/data
        // [HttpPost("{id}/data")]
        // public async Task<ActionResult> AddDeviceData(int id, [FromBody] DeviceData data)
        // {
        //     var device = await _context.Devices.FindAsync(id);
        //     if (device == null)
        //     {
        //         return NotFound(new {error = $"设备{id}未找到"});
        //     }
        //     
        //     data.DeviceId = id;
        //     data.Timestamp = DateTime.Now;
        //     _context.DeviceData.Add(data);
        //     
        //     //更新设备状态
        //     device.LastOnlineAt = DateTime.Now;
        //     device.Status=DeviceStatus.Online;
        //     
        //     await _context.SaveChangesAsync();
        //     
        //     return CreatedAtAction(nameof(GetDeviceById), new { id = id }, data);
        // }
        //
        //
        // //GET: api/device/{id}/data
        // [HttpGet("{id}/data")]
        // public async Task<ActionResult> GetDeviceData(int id,
        //     [FromQuery] string? dataType = null,
        //     [FromQuery] DateTime? startTime = null,
        //     [FromQuery] DateTime? endTime = null,
        //     [FromQuery] int limit = 100
        //     )
        // {
        //     var device = await _context.Devices.FindAsync(id);
        //     if (device == null)
        //     {
        //         return NotFound(new {error = $"设备{id}未找到"});
        //     }
        //     
        //     var query = _context.DeviceData.Where(d => d.DeviceId == id).AsQueryable();
        //     
        //    
        //     
        //     if (startTime.HasValue)
        //     {
        //         query = query.Where(d => d.Timestamp >= startTime.Value);
        //     }
        //     
        //     if (endTime.HasValue)
        //     {
        //         query = query.Where(d => d.Timestamp <= endTime.Value);
        //     }
        //     
        //     var data = await query.OrderByDescending(d => d.Timestamp)
        //         .Take(limit)
        //         .ToListAsync();
        //      
        //     return Ok(data);
        // }
        //     
        // //GET:api/device/statistics
        // [HttpGet("statistics")]
        // public async Task<ActionResult> GetDeviceStatistics()
        // {
        //     var totalDevices = await _context.Devices.CountAsync();
        //     var onlineDevices = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Online);
        //     var offlineDevices = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Offline);
        //
        //    var devicesByType =  await _context.Devices.GroupBy(d => d.Type)
        //         .Select(g => new
        //         {
        //             type = g.Key.ToString(),
        //             Count = g.Count()
        //         }).ToListAsync();
        //     
        //     var stats = new
        //     {
        //         TotalDevices = totalDevices,
        //         OnlineDevices = onlineDevices,
        //         OfflineDevices = offlineDevices,
        //         devicesByType = devicesByType
        //     };
        //
        //     return Ok(stats);
        // }

    }
}
