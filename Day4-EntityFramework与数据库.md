# Day 4: Entity Framework Core + SQLite数据库

> **学习目标**: 使用真实数据库持久化数据，掌握ORM基础
>
> **预计时间**: 2-3小时
>
> **前置知识**: 完成Day 1-3的学习

---

## 📚 今日知识点

### 核心内容

1. 什么是ORM（对象关系映射）
2. Entity Framework Core基础
3. SQLite数据库的使用
4. 数据库迁移（Migrations）
5. 异步数据库操作

---

## 🎯 什么是ORM？

**🔵 前端类比理解:**

- **ORM** (Object-Relational Mapping) ≈ Prisma / TypeORM / Sequelize
- 让你用**对象**的方式操作数据库，而不是写SQL语句
- Entity Framework Core ≈ Node.js的Prisma

**对比:**

```csharp
// ❌ 传统方式：写SQL
var sql = "SELECT * FROM Devices WHERE Type = 1";
var devices = ExecuteSql(sql);

// ✅ EF Core方式：用LINQ
var devices = await _context.Devices
    .Where(d => d.Type == DeviceType.Sensor)
    .ToListAsync();
```

```javascript
// JavaScript/Prisma 类似写法
const devices = await prisma.device.findMany({
  where: { type: 'Sensor' }
});
```

---

## 🚀 Step 1: 创建项目并安装EF Core

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2

# 创建新项目
dotnet new webapi -n Day4DatabaseAPI
cd Day4DatabaseAPI

# 安装EF Core相关包
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# 安装EF Core工具（用于迁移）
dotnet tool install --global dotnet-ef

# 验证安装
dotnet ef
```

**📦 包说明:**

- `Microsoft.EntityFrameworkCore.Sqlite` - SQLite数据库提供程序
- `Microsoft.EntityFrameworkCore.Design` - 设计时工具（用于迁移）
- `dotnet-ef` - EF Core命令行工具

---

## 📦 Step 2: 创建数据模型

创建 `Models/Device.cs`：

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Day4DatabaseAPI.Models
{
    public enum DeviceType
    {
        Sensor = 1,
        Controller = 2,
        Gateway = 3,
        Actuator = 4
    }

    public enum DeviceStatus
    {
        Offline = 0,
        Online = 1,
        Error = 2
    }

    public class Device
    {
        [Key]  // 主键
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // 自增
        public int Id { get; set; }

        [Required]  // 非空
        [MaxLength(100)]  // 最大长度
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DeviceType Type { get; set; }

        public DeviceStatus Status { get; set; } = DeviceStatus.Offline;

        [Required]
        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastOnlineAt { get; set; }

        // 导航属性：一对多
        public virtual ICollection<DeviceData> DataRecords { get; set; } = new List<DeviceData>();
    }

    public class DeviceData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int DeviceId { get; set; }  // 外键

        [Required]
        [MaxLength(50)]
        public string DataType { get; set; } = string.Empty;

        [Required]
        public double Value { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // 导航属性：多对一
        [ForeignKey("DeviceId")]
        public virtual Device? Device { get; set; }
    }
}
```

**📝 EF Core特性（Attributes）说明:**

| 特性                    | 作用     | SQL等价            |
|-----------------------|--------|------------------|
| `[Key]`               | 标记主键   | `PRIMARY KEY`    |
| `[Required]`          | 非空     | `NOT NULL`       |
| `[MaxLength(100)]`    | 最大长度   | `VARCHAR(100)`   |
| `[ForeignKey]`        | 外键     | `FOREIGN KEY`    |
| `[DatabaseGenerated]` | 数据库生成值 | `AUTO_INCREMENT` |
| `virtual`             | 延迟加载   | -                |

---

## 🗄️ Step 3: 创建DbContext

创建 `Data/AppDbContext.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Day4DatabaseAPI.Models;

namespace Day4DatabaseAPI.Data
{
    // DbContext是EF Core的核心类，代表数据库会话
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet代表数据库中的表
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceData> DeviceData { get; set; }

        // 配置模型（Fluent API）
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置Device表
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
            modelBuilder.Entity<Device>().HasData(
                new Device
                {
                    Id = 1,
                    Name = "温度传感器-01",
                    Description = "车间温度监控",
                    Type = DeviceType.Sensor,
                    Status = DeviceStatus.Online,
                    IpAddress = "192.168.1.100",
                    Port = 502,
                    CreatedAt = DateTime.Now,
                    LastOnlineAt = DateTime.Now
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
                    CreatedAt = DateTime.Now,
                    LastOnlineAt = DateTime.Now
                }
            );
        }
    }
}
```

**🔵 前端对比理解:**

```typescript
// Prisma Schema 等价
model Device {
  id            Int           @id @default(autoincrement())
  name          String        @db.VarChar(100)
  description   String?       @db.VarChar(500)
  type          DeviceType
  status        DeviceStatus  @default(Offline)
  ipAddress     String        @db.VarChar(50)
  port          Int
  createdAt     DateTime      @default(now())
  lastOnlineAt  DateTime?
  dataRecords   DeviceData[]  // 一对多关系
  
  @@index([ipAddress])
  @@index([type])
}

model DeviceData {
  id        Int      @id @default(autoincrement())
  deviceId  Int
  dataType  String
  value     Float
  unit      String
  timestamp DateTime @default(now())
  device    Device   @relation(fields: [deviceId], references: [id], onDelete: Cascade)
  
  @@index([deviceId])
  @@index([timestamp])
}
```

**📝 关键概念:**

1. **DbContext** - 数据库会话（类似Prisma Client）
2. **DbSet<T>** - 代表一个表（类似Prisma的model）
3. **OnModelCreating** - 配置模型（类似Prisma的schema）
4. **Fluent API** - 链式配置API
5. **种子数据** - 初始数据（类似seed.ts）

---

## ⚙️ Step 4: 配置数据库连接

修改 `Program.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Day4DatabaseAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// 添加DbContext服务
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

创建 `appsettings.json`（或修改现有的）：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=devices.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

**📝 说明:**

- `ConnectionStrings` - 数据库连接字符串
- `Data Source=devices.db` - SQLite数据库文件名
- `AddDbContext` - 注册DbContext到依赖注入容器

---

## 🔄 Step 5: 创建数据库迁移

```bash
# 1. 创建第一个迁移
dotnet ef migrations add InitialCreate

# 2. 应用迁移到数据库
dotnet ef database update
```

**执行后你会看到:**

- `Migrations/` 文件夹 - 包含迁移文件
- `devices.db` 文件 - SQLite数据库文件

**🔵 前端对比:**

```bash
# Prisma 等价命令
npx prisma migrate dev --name init
npx prisma generate
```

**📝 迁移命令:**

```bash
# 常用EF Core命令
dotnet ef migrations add <名称>           # 创建迁移
dotnet ef database update                 # 应用迁移
dotnet ef migrations remove               # 删除最后一个迁移
dotnet ef database drop                   # 删除数据库
dotnet ef migrations list                 # 查看所有迁移
```

---

## 🎮 Step 6: 创建使用DbContext的Controller

创建 `Controllers/DeviceController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Day4DatabaseAPI.Data;
using Day4DatabaseAPI.Models;

namespace Day4DatabaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly AppDbContext _context;

        // 通过依赖注入获取DbContext
        public DeviceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/device
        [HttpGet]
        public async Task<IActionResult> GetAllDevices(
            [FromQuery] DeviceType? type = null,
            [FromQuery] DeviceStatus? status = null)
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

        // GET: api/device/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceById(int id)
        {
            // Include: 加载关联数据（类似SQL的JOIN）
            var device = await _context.Devices
                .Include(d => d.DataRecords.OrderByDescending(dd => dd.Timestamp).Take(10))
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            return Ok(device);
        }

        // POST: api/device
        [HttpPost]
        public async Task<IActionResult> CreateDevice([FromBody] Device device)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            device.CreatedAt = DateTime.Now;
            device.Status = DeviceStatus.Offline;

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();  // 保存到数据库

            return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, device);
        }

        // PUT: api/device/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] Device device)
        {
            if (id != device.Id)
            {
                return BadRequest(new { error = "ID不匹配" });
            }

            var existingDevice = await _context.Devices.FindAsync(id);

            if (existingDevice == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            // 更新字段
            existingDevice.Name = device.Name;
            existingDevice.Description = device.Description;
            existingDevice.Status = device.Status;
            existingDevice.IpAddress = device.IpAddress;
            existingDevice.Port = device.Port;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // 并发冲突处理
                return Conflict(new { error = "数据已被其他用户修改" });
            }

            return Ok(existingDevice);
        }

        // DELETE: api/device/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/device/{id}/data
        [HttpPost("{id}/data")]
        public async Task<IActionResult> AddDeviceData(int id, [FromBody] DeviceData data)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
            {
                return NotFound(new { error = $"设备ID {id} 不存在" });
            }

            data.DeviceId = id;
            data.Timestamp = DateTime.Now;

            _context.DeviceData.Add(data);

            // 更新设备状态
            device.LastOnlineAt = DateTime.Now;
            device.Status = DeviceStatus.Online;

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeviceById), new { id = id }, data);
        }

        // GET: api/device/{id}/data
        [HttpGet("{id}/data")]
        public async Task<IActionResult> GetDeviceData(
            int id,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] int limit = 100)
        {
            var query = _context.DeviceData
                .Where(dd => dd.DeviceId == id);

            if (startTime.HasValue)
            {
                query = query.Where(dd => dd.Timestamp >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(dd => dd.Timestamp <= endTime.Value);
            }

            var data = await query
                .OrderByDescending(dd => dd.Timestamp)
                .Take(limit)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/device/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var totalDevices = await _context.Devices.CountAsync();
            var onlineDevices = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Online);
            var offlineDevices = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.Offline);
            
            var devicesByType = await _context.Devices
                .GroupBy(d => d.Type)
                .Select(g => new { 
                    Type = g.Key.ToString(), 
                    Count = g.Count() 
                })
                .ToListAsync();

            return Ok(new
            {
                totalDevices,
                onlineDevices,
                offlineDevices,
                devicesByType
            });
        }
    }
}
```

**📝 关键方法对比:**

| EF Core方法               | SQL等价              | JavaScript/Prisma等价 |
|-------------------------|--------------------|---------------------|
| `ToListAsync()`         | `SELECT *`         | `findMany()`        |
| `FirstOrDefaultAsync()` | `SELECT TOP 1`     | `findFirst()`       |
| `FindAsync(id)`         | `SELECT WHERE id=` | `findUnique()`      |
| `Add()`                 | `INSERT`           | `create()`          |
| `Remove()`              | `DELETE`           | `delete()`          |
| `SaveChangesAsync()`    | `COMMIT`           | (自动提交)              |
| `Include()`             | `JOIN`             | `include`           |
| `Where()`               | `WHERE`            | `where`             |
| `OrderBy()`             | `ORDER BY`         | `orderBy`           |
| `GroupBy()`             | `GROUP BY`         | `groupBy`           |
| `CountAsync()`          | `COUNT(*)`         | `count()`           |

**📝 异步方法（async/await）:**

```csharp
// ❌ 同步方法（阻塞线程）
var devices = _context.Devices.ToList();

// ✅ 异步方法（不阻塞线程）
var devices = await _context.Devices.ToListAsync();
```

类似JavaScript：

```javascript
// ❌ 同步
const data = fs.readFileSync('file.txt');

// ✅ 异步
const data = await fs.promises.readFile('file.txt');
```

---

## 🧪 Step 7: 测试

```bash
# 运行项目
dotnet watch run

# 测试创建设备
curl -X POST http://localhost:5000/api/device \
  -H "Content-Type: application/json" \
  -d '{
    "name": "湿度传感器-01",
    "description": "仓库湿度监控",
    "type": 1,
    "ipAddress": "192.168.1.105",
    "port": 502
  }'

# 获取所有设备
curl http://localhost:5000/api/device

# 获取统计信息
curl http://localhost:5000/api/device/statistics
```

---

## 📝 今日总结

### ✅ 你学会了：

- [x] Entity Framework Core的基础
- [x] 使用SQLite数据库
- [x] 数据库迁移（Migrations）
- [x] DbContext的配置和使用
- [x] 异步数据库操作（async/await）
- [x] 导航属性和Include
- [x] LINQ查询数据库

### 🔑 关键对比：

| EF Core              | Prisma           | 说明     |
|----------------------|------------------|--------|
| `DbContext`          | `PrismaClient`   | 数据库客户端 |
| `DbSet<T>`           | `model`          | 表/模型   |
| `OnModelCreating`    | `schema.prisma`  | 模型配置   |
| `migrations add`     | `migrate dev`    | 创建迁移   |
| `database update`    | `migrate deploy` | 应用迁移   |
| `Include()`          | `include`        | 关联查询   |
| `SaveChangesAsync()` | (自动)             | 保存更改   |

---

## 🎯 明日预告：Day 5 - 服务层和依赖注入

明天你将学习：

- 什么是依赖注入（DI）
- 服务层（Service Layer）设计
- Repository模式
- 业务逻辑与数据访问分离
- ASP.NET Core的DI容器

---

## 💾 作业

1. 添加软删除功能（不真正删除，只标记为已删除）
2. 添加设备搜索（按名称、IP地址搜索）
3. 添加数据统计（设备数据的平均值、最大值、最小值）
4. 尝试查看 `devices.db` 文件（用SQLite浏览器工具）

---

**数据库掌握了，离实战越来越近！🚀**


