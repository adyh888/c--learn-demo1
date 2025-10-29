# Day 6: 异步编程(async/await)和LINQ深入

> **学习目标**: 深入理解异步编程、掌握LINQ高级查询
>
> **预计时间**: 2-3小时

---

## 📚 今日知识点

### 1. async/await深入理解

**🔵 与JavaScript对比:**

```javascript
// JavaScript async/await
async function fetchData() {
  const response = await fetch('/api/data');
  const data = await response.json();
  return data;
}
```

```csharp
// C# async/await（几乎一样！）
async Task<Data> FetchDataAsync() 
{
    var response = await httpClient.GetAsync("/api/data");
    var data = await response.Content.ReadAsAsync<Data>();
    return data;
}
```

**关键区别:**

- C#需要显式声明返回类型 `Task<T>`
- 异步方法名通常以 `Async` 结尾（约定）
- `Task` ≈ JavaScript的 `Promise`

### 2. Task vs Task<T>

```csharp
// Task - 无返回值（类似 Promise<void>）
public async Task DoSomethingAsync()
{
    await Task.Delay(1000);
}

// Task<T> - 有返回值（类似 Promise<T>）
public async Task<string> GetDataAsync()
{
    await Task.Delay(1000);
    return "data";
}

// 同步方法（不推荐阻塞）
public string GetData()
{
    Thread.Sleep(1000);  // ❌ 阻塞线程
    return "data";
}
```

### 3. 异步的最佳实践

```csharp
// ✅ 好的做法：一路异步到底
[HttpGet]
public async Task<IActionResult> GetDevices()
{
    var devices = await _service.GetAllDevicesAsync();
    return Ok(devices);
}

// ❌ 不好：混用同步和异步
[HttpGet]
public IActionResult GetDevices()
{
    var devices = _service.GetAllDevicesAsync().Result;  // ❌ 容易死锁
    return Ok(devices);
}
```

### 4. 并行执行多个异步任务

```csharp
// ❌ 串行执行（慢）
var devices = await _deviceRepo.GetAllAsync();
var statistics = await _deviceRepo.GetStatisticsAsync();
var recentData = await _dataRepo.GetRecentAsync();
// 总耗时 = T1 + T2 + T3

// ✅ 并行执行（快）
var devicesTask = _deviceRepo.GetAllAsync();
var statisticsTask = _deviceRepo.GetStatisticsAsync();
var recentDataTask = _dataRepo.GetRecentAsync();

await Task.WhenAll(devicesTask, statisticsTask, recentDataTask);

var devices = devicesTask.Result;
var statistics = statisticsTask.Result;
var recentData = recentDataTask.Result;
// 总耗时 = Max(T1, T2, T3)
```

**🔵 JavaScript等价:**

```javascript
// 串行
const devices = await getDevices();
const statistics = await getStatistics();

// 并行
const [devices, statistics] = await Promise.all([
  getDevices(),
  getStatistics()
]);
```

---

## 🎯 LINQ深入：30个常用查询

### 基础查询

```csharp
var devices = _context.Devices;

// 1. Where - 筛选（类似 .filter）
var sensors = devices.Where(d => d.Type == DeviceType.Sensor);

// 2. Select - 映射（类似 .map）
var names = devices.Select(d => d.Name);

// 3. First / FirstOrDefault
var first = devices.FirstOrDefault(d => d.Id == 1);  // 找不到返回null

// 4. Single / SingleOrDefault（确保只有一个结果）
var device = devices.SingleOrDefault(d => d.IpAddress == "192.168.1.100");

// 5. Any - 是否存在（类似 .some）
bool hasOnline = devices.Any(d => d.Status == DeviceStatus.Online);

// 6. All - 是否全部满足（类似 .every）
bool allOnline = devices.All(d => d.Status == DeviceStatus.Online);

// 7. Count
int count = devices.Count();
int onlineCount = devices.Count(d => d.Status == DeviceStatus.Online);

// 8. Take / Skip - 分页
var page1 = devices.OrderBy(d => d.Id).Skip(0).Take(10);
var page2 = devices.OrderBy(d => d.Id).Skip(10).Take(10);

// 9. OrderBy / OrderByDescending
var sorted = devices.OrderBy(d => d.CreatedAt);
var sortedDesc = devices.OrderByDescending(d => d.CreatedAt);

// 10. ThenBy - 多级排序
var multiSort = devices
    .OrderBy(d => d.Type)
    .ThenByDescending(d => d.CreatedAt);
```

### 聚合查询

```csharp
// 11. Sum
decimal totalValue = deviceData.Sum(d => d.Value);

// 12. Average
double avgValue = deviceData.Average(d => d.Value);

// 13. Min / Max
double minValue = deviceData.Min(d => d.Value);
double maxValue = deviceData.Max(d => d.Value);

// 14. GroupBy - 分组（类似 SQL GROUP BY）
var groupedByType = devices
    .GroupBy(d => d.Type)
    .Select(g => new {
        Type = g.Key,
        Count = g.Count(),
        Devices = g.ToList()
    });

// 15. Distinct - 去重
var uniqueTypes = devices.Select(d => d.Type).Distinct();
```

### 关联查询

```csharp
// 16. Include - 加载关联数据（类似 SQL JOIN）
var devicesWithData = _context.Devices
    .Include(d => d.DataRecords)
    .ToList();

// 17. ThenInclude - 多级Include
var devicesWithFilteredData = _context.Devices
    .Include(d => d.DataRecords.Where(dd => dd.Timestamp > DateTime.Now.AddDays(-1)))
    .ToList();

// 18. Join - 手动连接
var joined = _context.Devices
    .Join(_context.DeviceData,
        device => device.Id,
        data => data.DeviceId,
        (device, data) => new { device.Name, data.Value });

// 19. GroupJoin - 分组连接
var groupJoined = _context.Devices
    .GroupJoin(_context.DeviceData,
        device => device.Id,
        data => data.DeviceId,
        (device, dataGroup) => new {
            Device = device,
            DataCount = dataGroup.Count()
        });
```

### 条件查询

```csharp
// 20. 动态筛选
var query = _context.Devices.AsQueryable();

if (type.HasValue)
    query = query.Where(d => d.Type == type.Value);

if (status.HasValue)
    query = query.Where(d => d.Status == status.Value);

if (!string.IsNullOrEmpty(keyword))
    query = query.Where(d => d.Name.Contains(keyword));

var result = await query.ToListAsync();

// 21. 复杂条件
var complex = devices.Where(d => 
    (d.Type == DeviceType.Sensor && d.Status == DeviceStatus.Online) ||
    (d.Type == DeviceType.Gateway && d.LastOnlineAt > DateTime.Now.AddHours(-1))
);
```

### 投影和转换

```csharp
// 22. 匿名对象投影
var projection = devices.Select(d => new {
    d.Id,
    d.Name,
    TypeName = d.Type.ToString(),
    IsOnline = d.Status == DeviceStatus.Online
});

// 23. DTO投影
var dtos = devices.Select(d => new DeviceResponseDto {
    Id = d.Id,
    Name = d.Name,
    Type = d.Type.ToString()
});

// 24. ToDictionary - 转字典
var dict = devices.ToDictionary(d => d.Id, d => d.Name);
// 使用: dict[1] => "设备名称"
```

### 集合操作

```csharp
// 25. Union - 并集
var union = onlineDevices.Union(recentlyOfflineDevices);

// 26. Intersect - 交集
var intersect = sensorsDevices.Intersect(onlineDevices);

// 27. Except - 差集
var offline = allDevices.Except(onlineDevices);

// 28. Concat - 连接
var combined = devices1.Concat(devices2);
```

### 高级查询

```csharp
// 29. 子查询
var devicesWithManyData = _context.Devices
    .Where(d => _context.DeviceData
        .Count(dd => dd.DeviceId == d.Id) > 100);

// 30. 原始SQL查询（需要时）
var devices = await _context.Devices
    .FromSqlRaw("SELECT * FROM Devices WHERE Status = 'Online'")
    .ToListAsync();
```

---

## 💡 性能优化技巧

### 1. 延迟执行 vs 立即执行

```csharp
// 延迟执行（只是构建查询，未执行）
IQueryable<Device> query = _context.Devices.Where(d => d.Type == DeviceType.Sensor);

// 立即执行（查询数据库）
List<Device> devices = query.ToList();
int count = query.Count();
bool any = query.Any();
```

### 2. AsNoTracking - 只读查询

```csharp
// ❌ 默认：跟踪实体（用于更新）
var devices = await _context.Devices.ToListAsync();

// ✅ 只读：不跟踪，性能更好
var devices = await _context.Devices
    .AsNoTracking()
    .ToListAsync();
```

### 3. 避免N+1查询

```csharp
// ❌ N+1问题
var devices = await _context.Devices.ToListAsync();
foreach (var device in devices)
{
    // 每个设备都查询一次数据库！
    var dataCount = await _context.DeviceData
        .CountAsync(d => d.DeviceId == device.Id);
}

// ✅ 一次性加载
var devices = await _context.Devices
    .Select(d => new {
        Device = d,
        DataCount = d.DataRecords.Count
    })
    .ToListAsync();
```

### 4. 分页最佳实践

```csharp
public async Task<PagedResult<Device>> GetPagedDevicesAsync(
    int page, int pageSize)
{
    var query = _context.Devices
        .AsNoTracking()
        .OrderBy(d => d.Id);

    var totalCount = await query.CountAsync();
    
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<Device>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

---

## 🎮 实战：复杂查询示例

### 示例1：设备监控仪表板

```csharp
public async Task<DashboardDto> GetDashboardAsync()
{
    // 并行执行多个查询
    var totalDevicesTask = _context.Devices.CountAsync();
    var onlineDevicesTask = _context.Devices.CountAsync(d => d.Status == DeviceStatus.Online);
    
    var recentDataTask = _context.DeviceData
        .Where(d => d.Timestamp > DateTime.Now.AddHours(-1))
        .OrderByDescending(d => d.Timestamp)
        .Take(20)
        .ToListAsync();
    
    var devicesByTypeTask = _context.Devices
        .GroupBy(d => d.Type)
        .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
        .ToListAsync();

    await Task.WhenAll(totalDevicesTask, onlineDevicesTask, recentDataTask, devicesByTypeTask);

    return new DashboardDto
    {
        TotalDevices = totalDevicesTask.Result,
        OnlineDevices = onlineDevicesTask.Result,
        RecentData = recentDataTask.Result,
        DevicesByType = devicesByTypeTask.Result.ToDictionary(x => x.Type, x => x.Count)
    };
}
```

### 示例2：数据趋势分析

```csharp
public async Task<List<TrendData>> GetDataTrendAsync(int deviceId, int hours = 24)
{
    var startTime = DateTime.Now.AddHours(-hours);
    
    var trend = await _context.DeviceData
        .Where(d => d.DeviceId == deviceId && d.Timestamp >= startTime)
        .GroupBy(d => new {
            Hour = d.Timestamp.Hour,
            Date = d.Timestamp.Date
        })
        .Select(g => new TrendData
        {
            Time = g.Key.Date.AddHours(g.Key.Hour),
            AverageValue = g.Average(d => d.Value),
            MinValue = g.Min(d => d.Value),
            MaxValue = g.Max(d => d.Value),
            Count = g.Count()
        })
        .OrderBy(t => t.Time)
        .ToListAsync();

    return trend;
}
```

---

## 📝 今日总结

### ✅ 你学会了：

- [x] async/await深入理解
- [x] Task和Task<T>
- [x] 并行异步操作
- [x] LINQ 30个常用方法
- [x] 性能优化技巧
- [x] 复杂查询的编写

### 🔑 LINQ vs JavaScript对比：

| LINQ                | JavaScript  | 说明 |
|---------------------|-------------|----|
| `.Where()`          | `.filter()` | 筛选 |
| `.Select()`         | `.map()`    | 映射 |
| `.FirstOrDefault()` | `.find()`   | 查找 |
| `.Any()`            | `.some()`   | 存在 |
| `.All()`            | `.every()`  | 全部 |
| `.Count()`          | `.length`   | 计数 |
| `.OrderBy()`        | `.sort()`   | 排序 |
| `.GroupBy()`        | -           | 分组 |
| `.Sum()`            | `.reduce()` | 求和 |

---

## 🎯 明日预告：Day 7 - 中间件和异常处理

明天你将学习：

- ASP.NET Core中间件管道
- 全局异常处理
- 日志系统
- 请求/响应拦截

---

## 💾 作业

1. 实现一个通用的分页查询方法
2. 优化设备列表查询（避免N+1）
3. 实现设备数据的时间范围查询
4. 对比同步和异步的性能差异

---

**异步编程和LINQ掌握了，你已经是半个高手了！🎉**


