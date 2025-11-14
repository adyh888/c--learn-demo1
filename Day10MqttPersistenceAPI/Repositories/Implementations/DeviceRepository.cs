using Microsoft.EntityFrameworkCore;
using Day10MqttPersistenceAPI.Data;
using Day10MqttPersistenceAPI.Models;
using Day10MqttPersistenceAPI.Repositories.Interfaces;

namespace Day10MqttPersistenceAPI.Repositories.Implementations;

public class DeviceRepository:IDeviceRepository
{
    private  readonly AppDbContext _context;
    
    //构造器
    public DeviceRepository(AppDbContext context)
    {
        _context = context;
    }
    
    //查询
    public async Task<List<Device>> GetAllAsync()
    {
        return await _context.Devices.ToListAsync();
    }
    
    
    public async Task<Device?> GetByIdAsync(int id)
    {
        return await _context.Devices.FindAsync(id);
    }
    
    
    public async Task<Device?> GetByIdWithDataAsync(int id)
    {
        return await _context.Devices
            .Include(d => d.DataRecords.OrderByDescending(dr => dr.Timestamp).Take(10))
            .FirstOrDefaultAsync(d => d.Id == id);
    }
    
    
    public async Task<List<Device>> GetByTypeAsync(DeviceType type)
    {
        return await _context.Devices
            .Where(d => d.Type == type)
            .ToListAsync();
    }
    
    public async Task<List<Device>> GetByStatusAsync(DeviceStatus status)
    {
        return await _context.Devices
            .Where(d => d.Status == status)
            .ToListAsync();
    }
    
    
    public async Task<bool> ExistsByIpAsync(string ipAddress)
    {
        return await _context.Devices
            .AnyAsync(d => d.IpAddress == ipAddress);
    }
    
    
    public async Task<Device> CreateAsync(Device device)
    {
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        return device;
    }
    
    
    public async Task<Device> UpdateAsync(Device device)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync();
        return device;
    }
    
    
    public async Task DeleteAsync(Device device)
    {
        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
    }
    
    
    public async Task<int> CountAsync()
    {
        return await _context.Devices.CountAsync();
    }
    
    public async Task<int> CountByStatusAsync(DeviceStatus status)
    {
        return await _context.Devices
            .CountAsync(d => d.Status == status);
    }
    
    
}