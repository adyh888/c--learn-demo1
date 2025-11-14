using Microsoft.EntityFrameworkCore;
using Day10MqttPersistenceAPI.Models;


namespace Day10MqttPersistenceAPI.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    //DbSet代表数据库中的表
	public DbSet<Device> Devices { get; set; }                   // 新增
    public DbSet<DeviceData> DeviceData { get; set; }            // 新增
  	public DbSet<DeviceMessage> DeviceMessages { get; set; } 
	public DbSet<DeviceStatistics> DeviceStatistics { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 可以在这里配置实体的属性和关系
		//索引优化
		modelBuilder.Entity<DeviceMessage>(entity =>{
		entity.HasIndex(e =>e.DeviceId);
		entity.HasIndex(e =>e.DeviceTimestamp);
		entity.HasIndex(e =>new {e.DeviceId,e.DataType,e.DeviceTimestamp});
        });

        modelBuilder.Entity<DeviceStatistics>(entity =>{
        entity.HasIndex(e =>e.DeviceId);
		entity.HasIndex(e => new {e.DeviceId,e.PeriodStart});
});



    }

    
    

    
    
}