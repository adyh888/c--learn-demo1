using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Day3DeviceAPI.Models;
using Day3DeviceAPI.Data;
using Day3DeviceAPI.DTOs;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        // GET: api/device
        [HttpGet]
        public IActionResult GetAllDevices(
            [FromQuery] DeviceType? type = null,
            [FromQuery] DeviceStatus? status = null
        )
        {
            var devices = DeviceStore.Devices.AsQueryable();
            
            //按类型筛选
            if (type.HasValue)
            {
                devices =devices.Where(d => d.Type == type.Value);
            }
            
            //按状态筛选
            if (status.HasValue)
            {
                devices =devices.Where(d => d.Status == status.Value);
            }
            
            //转换为DTO
            var response = devices.Select(d => new DeviceResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Type = d.Type.ToString(),
                Status = d.Status.ToString(),
                IpAddress = d.IpAddress,
                Port = d.Port,
                CreatedAt = d.CreatedAt,
                LastOnlineAt = d.LastOnlineAt,
                DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == d.Id)
            }).ToList();
                
                return Ok(response);
            
        }

        // GET: api/device/{id}
        [HttpGet("{id}")]
        public IActionResult GetDeviceById(int id)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
            {
                return NotFound(new { Message = $"设备ID{id}未找到" });
            }
            
            //获取最近10条数据
           var recentData =  DeviceStore.DeviceDataRecords
                .Where(dd => dd.DeviceId == device.Id)
                .OrderByDescending(dd => dd.Timestamp)
                .Take(10)
                .Select(dd => new DeviceDataDto
                {
                    Id = dd.Id,
                    DataType = dd.DataType,
                    Value = dd.Value,
                    Unit = dd.Unit,
                    Timestamp = dd.Timestamp
                })
                .ToList();
            

            var response = new DeviceDetailDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Type = device.Type.ToString(),
                Status = device.Status.ToString(),
                IpAddress = device.IpAddress,
                Port = device.Port,
                CreatedAt = device.CreatedAt,
                LastOnlineAt = device.LastOnlineAt,
                DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == id),
                RecentData = recentData
            };

            return Ok(response);
        }


        // POST: api/device
        [HttpPost]
        public IActionResult CreateDevice([FromBody] CreateDeviceDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newDevice = new Device
            {
                Id = DeviceStore.GetNextDeviceId(),
                Name = createDto.Name,
                Description = createDto.Description,
                Type = createDto.Type,
                Status = DeviceStatus.Offline,  // 新建设备默认离线
                IpAddress = createDto.IpAddress,
                Port = createDto.Port,
                CreatedAt = DateTime.Now,
                LastOnlineAt = null
            };

            DeviceStore.Devices.Add(newDevice);

            var response = new DeviceResponseDto
            {
                Id = newDevice.Id,
                Name = newDevice.Name,
                Description = newDevice.Description,
                Type = newDevice.Type.ToString(),
                Status = newDevice.Status.ToString(),
                IpAddress = newDevice.IpAddress,
                Port = newDevice.Port,
                CreatedAt = newDevice.CreatedAt,
                LastOnlineAt = newDevice.LastOnlineAt,
                DataRecordCount = 0
            };

            return CreatedAtAction(nameof(GetDeviceById), new { id = newDevice.Id }, response);
        }
        
        // PUT: api/device/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateDevice(int id, [FromBody] UpdateDeviceDto updateDto)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
            {
                return NotFound(new { Message = $"设备ID{id}未找到" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //更新字段
            if (!string.IsNullOrEmpty(updateDto.Name))
            {
                device.Name = updateDto.Name;
            }

            if (!string.IsNullOrEmpty(updateDto.Description))
            {
                device.Description = updateDto.Description;
            }

            device.Status = updateDto.Status;
            

            if (!string.IsNullOrEmpty(updateDto.IpAddress))
            {
                device.IpAddress = updateDto.IpAddress;
            }

            if (updateDto.Port != 0)
            {
                device.Port = updateDto.Port;
            }
            
            var response = new DeviceResponseDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Type = device.Type.ToString(),
                Status = device.Status.ToString(),
                IpAddress = device.IpAddress,
                Port = device.Port,
                CreatedAt = device.CreatedAt,
                LastOnlineAt = device.LastOnlineAt,
                DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == device.Id)
            };

            return Ok(response);
        }
        
        
        // DELETE: api/device/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteDevice(int id)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
            {
                return NotFound(new { Message = $"设备ID{id}未找到" });
            }

            //删除关联的数据记录
            DeviceStore.DeviceDataRecords.RemoveAll(dd => dd.DeviceId == device.Id);
            //删除设备
            DeviceStore.Devices.Remove(device);

            return NoContent();
        }

        
        //添加设备统计API： 返回：总设备数、在线设备数、离线设备数、各类型设备数
        [HttpGet("statistics")]
        public IActionResult GetDeviceStatistics()
        {
            var totalDevices = DeviceStore.Devices.Count;
            var onlineDevices = DeviceStore.Devices.Count(d => d.Status == DeviceStatus.Online);
            var offlineDevices = DeviceStore.Devices.Count(d => d.Status == DeviceStatus.Offline);
            var sensorCount = DeviceStore.Devices.Count(d => d.Type == DeviceType.Sensor);
            var controllerCount = DeviceStore.Devices.Count(d => d.Type == DeviceType.Controller);
            var gatewayCount = DeviceStore.Devices.Count(d => d.Type == DeviceType.Gateway);
            var actuatorCount = DeviceStore.Devices.Count(d => d.Type == DeviceType.Actuator);

            var stats = new
            {
                TotalDevices = totalDevices,
                OnlineDevices = onlineDevices,
                OfflineDevices = offlineDevices,
                SensorCount = sensorCount,
                ControllerCount = controllerCount,
                GatewayCount = gatewayCount,
                ActuatorCount = actuatorCount
            };

            return Ok(stats);
        }
        
        //添加设备搜索：按名称或描述搜索
        [HttpGet("search")]
        public IActionResult SearchDevices([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { Message = "搜索查询不能为空" });
            }

            var results = DeviceStore.Devices
                .Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            d.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(d => new DeviceResponseDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    Type = d.Type.ToString(),
                    Status = d.Status.ToString(),
                    IpAddress = d.IpAddress,
                    Port = d.Port,
                    CreatedAt = d.CreatedAt,
                    LastOnlineAt = d.LastOnlineAt,
                    DataRecordCount = DeviceStore.DeviceDataRecords.Count(dd => dd.DeviceId == d.Id)
                })
                .ToList();

            return Ok(results);
        }
        
        //添加批量更新操作：批量修改设备状态
        [HttpPut("batch/updateStatus")]
        public IActionResult BatchUpdateDeviceStatus([FromBody] BatchUpdateDeviceStatusDto batchDto)
        {
            if (batchDto.DeviceIds == null || !batchDto.DeviceIds.Any())
            {
                return BadRequest(new { Message = "设备ID列表不能为空" });
            }

            foreach (var id in batchDto.DeviceIds)
            {
                var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);
                if (device != null)
                {
                    device.Status = batchDto.Status;
                }
            }

            return NoContent();
        }
        

        // POST: api/device/{id}/data
        [HttpPost("{id}/data")]
        public IActionResult AddDeviceData(int id, [FromBody] CreateDeviceDataDto dataDto)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
            {
                return NotFound(new { Message = $"设备ID{id}未找到" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newData = new DeviceData
            {
                Id = DeviceStore.GetNextDataId(),
                DeviceId = id,
                DataType = dataDto.DataType,
                Value = dataDto.Value,
                Unit = dataDto.Unit,
                Timestamp = DateTime.Now
            };

            DeviceStore.DeviceDataRecords.Add(newData);

            // 更新设备最后在线时间
            device.LastOnlineAt = DateTime.Now;
            device.Status = DeviceStatus.Online;


            return CreatedAtAction(nameof(GetDeviceById), new { id = id }, newData);
        }



        // GET: api/device/{id}/data
        [HttpGet("{id}/data")]
        public IActionResult GetDeviceData(int id,
            [FromQuery] int limit = 50)
        {
            var device = DeviceStore.Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
            {
                return NotFound(new { Message = $"设备ID{id}未找到" });
            }

            var data = DeviceStore.DeviceDataRecords
                .Where(dd => dd.DeviceId == id)
                .OrderByDescending(dd => dd.Timestamp)
                .Take(limit)
                .Select(dd => new DeviceDataDto
                {
                    Id = dd.Id,
                    DataType = dd.DataType,
                    Value = dd.Value,
                    Unit = dd.Unit,
                    Timestamp = dd.Timestamp
                })
                .ToList();


            return Ok(data);
        }

    }
}
