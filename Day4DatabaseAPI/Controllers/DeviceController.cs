using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Day4DatabaseAPI.Models;
using Day4DatabaseAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        
        private readonly AppDbContext _context;
        
        //通过依赖注入获取DbContext
        public DeviceController(AppDbContext context)
        {
            _context = context;
        }
        
        // GET: api/Device
        [HttpGet]
        public async Task<ActionResult> GetAllDevices(
            [FromQuery] DeviceType? type =null,
            [FromQuery] DeviceStatus? status =null
            )
        {
            var query = _context.Devices.AsQueryable();
            
            if (type.HasValue)
            {
                query = query.Where(d => d.Type == type.Value);
            }
            
            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }
            
            var devices = await query.ToListAsync();
            return Ok(devices);
        }
        
        //GET: api/Device/5
        [HttpGet("{id}")]
        public async Task<ActionResult> GetDeviceById(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }

            return Ok(device);
        }
        
        
        //POST: api/Device
        [HttpPost]
        public async Task<ActionResult> CreateDevice([FromBody] Device device)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            _context.Devices.Add(device);
            await _context.SaveChangesAsync(); //保存到数据库
            return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, device);
        }
        
        
        //PUT: api/Device/5
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDevice(int id, [FromBody] Device updatedDevice)
        {
            if (id != updatedDevice.Id)
            {
                return BadRequest("ID不匹配");
            }
            var existingDevice = await _context.Devices.FindAsync(id);
            if (existingDevice == null)
            {
                return NotFound(new {error = $"设备{id}未找到"});
            }
            
            // 更新字段
            existingDevice.Name = updatedDevice.Name;
            existingDevice.Description = updatedDevice.Description;
            existingDevice.Status = updatedDevice.Status;
            existingDevice.IpAddress = updatedDevice.IpAddress;
            existingDevice.Port = updatedDevice.Port;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Devices.Any(d => d.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return Ok(existingDevice);
        }
        
        
        //DELETE: api/Device/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound(new {error = $"设备{id}未找到"});
            }
            
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        //POST: api/device/{id}/data
        [HttpPost("{id}/data")]
        public async Task<ActionResult> AddDeviceData(int id, [FromBody] DeviceData data)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound(new {error = $"设备{id}未找到"});
            }
            
            data.DeviceId = id;
            data.Timestamp = DateTime.Now;
            _context.DeviceData.Add(data);
            
            //更新设备状态
            device.LastOnlineAt = DateTime.Now;
            device.Status=DeviceStatus.Online;
            
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetDeviceById), new { id = id }, data);
        }
        
        
        //GET: api/device/{id}/data
        [HttpGet("{id}/data")]
        public async Task<ActionResult> GetDeviceData(int id,
            [FromQuery] string? dataType = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] int limit = 100
            )
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound(new {error = $"设备{id}未找到"});
            }
            
            var query = _context.DeviceData.Where(d => d.DeviceId == id).AsQueryable();
            
           
            
            if (startTime.HasValue)
            {
                query = query.Where(d => d.Timestamp >= startTime.Value);
            }
            
            if (endTime.HasValue)
            {
                query = query.Where(d => d.Timestamp <= endTime.Value);
            }
            
            var data = await query.OrderByDescending(d => d.Timestamp)
                .Take(limit)
                .ToListAsync();
             
            return Ok(data);
        }
            
        //GET:api/device/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult> GetDeviceStatistics()
        {
            var totalDevices = await _context.Devices.CountAsync();
            var onlineDevices = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Online);
            var offlineDevices = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Offline);

           var devicesByType =  await _context.Devices.GroupBy(d => d.Type)
                .Select(g => new
                {
                    type = g.Key.ToString(),
                    Count = g.Count()
                }).ToListAsync();
            
            var stats = new
            {
                TotalDevices = totalDevices,
                OnlineDevices = onlineDevices,
                OfflineDevices = offlineDevices,
                devicesByType = devicesByType
            };

            return Ok(stats);
        }

    }
}
