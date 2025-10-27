using Microsoft.EntityFrameworkCore;
using Day7MiddlewareAPI.Models;


namespace Day7MiddlewareAPI.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    //DbSet代表数据库中的表
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceData> DeviceData { get; set; }
    
    
    //配置模型(Fluent API)
    protected  override  void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        //配置Device实体
        modelBuilder.Entity<Device>(entity =>
        {
            // 表名
            entity.ToTable("Devices");

            // 索引（提高查询性能）
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);

            // 枚举转换为字符串存储
            entity.Property(e => e.Type)
                .HasConversion<string>();
            entity.Property(e => e.Status)
                .HasConversion<string>();

            // 一对多关系
            entity.HasMany(e => e.DataRecords)
                .WithOne(e => e.Device)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);  // 级联删除
        });
        
        // 配置DeviceData表
        modelBuilder.Entity<DeviceData>(entity =>
        {
            entity.ToTable("DeviceData");

            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Timestamp);
        });

        // 种子数据（初始数据）
        SeedData(modelBuilder);
    }


    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>().HasData(new Device
            {
                Id = 1,
                Name = "温度传感器-01",
                Description = "车间温度监控",
                Type = DeviceType.Sensor,
                Status = DeviceStatus.Online,
                IpAddress = "192.168.1.100",
                Port = 502,
                CreatedAt = new DateTime(2025, 10, 20),
                LastOnlineAt = new DateTime(2025, 10, 20)
            },
            new Device
            {
                Id = 2,
                Name = "PLC控制器-01",
                Description = "生产线控制器",
                Type = DeviceType.Controller,
                Status = DeviceStatus.Online,
                IpAddress = "192.168.1.101",
                Port = 502,
                CreatedAt = new DateTime(2025, 10, 20),
                LastOnlineAt = new DateTime(2025, 10, 20)
            });
    }
    
    
}