# Day 7: 中间件(Middleware)和全局异常处理

> **学习目标**: 理解中间件管道、实现全局异常处理和日志
>
> **预计时间**: 2-3小时

---

## 📚 今日知识点

### 1. 什么是中间件？

**🔵 与Express.js对比:**

```javascript
// Express.js 中间件
app.use((req, res, next) => {
  console.log('请求时间:', Date.now());
  next();  // 调用下一个中间件
});

app.use(express.json());
app.use('/api', apiRoutes);
```

```csharp
// ASP.NET Core 中间件（Program.cs）
app.Use(async (context, next) => {
    Console.WriteLine($"请求时间: {DateTime.Now}");
    await next();  // 调用下一个中间件
});

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
```

**中间件管道示意图:**

```
HTTP请求
   ↓
┌─────────────────────┐
│  Middleware 1       │ → 日志记录
└─────────────────────┘
   ↓
┌─────────────────────┐
│  Middleware 2       │ → 异常处理
└─────────────────────┘
   ↓
┌─────────────────────┐
│  Middleware 3       │ → 身份验证
└─────────────────────┘
   ↓
┌─────────────────────┐
│  Controller/API     │ → 业务处理
└─────────────────────┘
   ↓
HTTP响应
```

---

## 🚀 Step 1: 创建全局异常处理中间件

创建 `Middleware/ExceptionHandlingMiddleware.cs`：

```csharp
using System.Net;
using System.Text.Json;

namespace Day7MiddlewareAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 调用下一个中间件
                await _next(context);
            }
            catch (Exception ex)
            {
                // 捕获所有异常
                _logger.LogError(ex, "发生未处理的异常");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = notFoundEx.Message;
                    response.StatusCode = 404;
                    break;

                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = validationEx.Message;
                    response.StatusCode = 400;
                    response.Errors = validationEx.Errors;
                    break;

                case UnauthorizedException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "未授权访问";
                    response.StatusCode = 401;
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "服务器内部错误";
                    response.StatusCode = 500;
                    break;
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    // 错误响应模型
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    // 自定义异常类
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class ValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; set; }

        public ValidationException(string message, Dictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}
```

**使用方法（在Service中抛出异常）:**

```csharp
public async Task<DeviceDetailDto> GetDeviceByIdAsync(int id)
{
    var device = await _deviceRepository.GetByIdAsync(id);

    if (device == null)
    {
        throw new NotFoundException($"设备ID {id} 不存在");
    }

    return MapToDetailDto(device);
}
```

**Controller变得更简洁:**

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetDeviceById(int id)
{
    // 不需要检查null，异常会被中间件捕获
    var device = await _deviceService.GetDeviceByIdAsync(id);
    return Ok(device);
}
```

---

## 📝 Step 2: 创建请求日志中间件

创建 `Middleware/RequestLoggingMiddleware.cs`：

```csharp
namespace Day7MiddlewareAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.Now;

            // 记录请求信息
            _logger.LogInformation(
                "收到请求: {Method} {Path} 来自 {IP}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            // 调用下一个中间件
            await _next(context);

            // 记录响应信息
            var duration = DateTime.Now - startTime;
            _logger.LogInformation(
                "完成响应: {Method} {Path} {StatusCode} 耗时 {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }
    }
}
```

**输出示例:**

```
收到请求: GET /api/device 来自 127.0.0.1
完成响应: GET /api/device 200 耗时 45ms
```

---

## ⏱️ Step 3: 创建性能监控中间件

创建 `Middleware/PerformanceMiddleware.cs`：

```csharp
using System.Diagnostics;

namespace Day7MiddlewareAPI.Middleware
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;
        private readonly int _warningThresholdMs = 1000;  // 1秒

        public PerformanceMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            if (elapsed > _warningThresholdMs)
            {
                _logger.LogWarning(
                    "⚠️ 慢请求: {Method} {Path} 耗时 {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    elapsed);
            }
        }
    }
}
```

---

## 🔧 Step 4: 创建CORS中间件配置

创建 `Middleware/CorsMiddleware.cs`：

```csharp
namespace Day7MiddlewareAPI.Middleware
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddCustomCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                // 开发环境：允许所有来源
                options.AddPolicy("Development", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });

                // 生产环境：指定来源
                options.AddPolicy("Production", builder =>
                {
                    builder.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
                           .WithMethods("GET", "POST", "PUT", "DELETE")
                           .WithHeaders("Content-Type", "Authorization")
                           .AllowCredentials();
                });
            });

            return services;
        }
    }
}
```

---

## ⚙️ Step 5: 配置中间件管道

修改 `Program.cs`：

```csharp
using Day7MiddlewareAPI.Middleware;
using Day7MiddlewareAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 配置服务
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加自定义CORS
builder.Services.AddCustomCors();

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// 配置中间件管道（顺序很重要！）
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// 1. 异常处理（必须在最前面，捕获所有后续中间件的异常）
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. HTTPS重定向
app.UseHttpsRedirection();

// 3. 性能监控
app.UseMiddleware<PerformanceMiddleware>();

// 4. 请求日志
app.UseMiddleware<RequestLoggingMiddleware>();

// 5. CORS（在路由之前）
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");

// 6. Swagger（仅开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 7. 静态文件（如果有）
// app.UseStaticFiles();

// 8. 路由
app.UseRouting();

// 9. 认证和授权
app.UseAuthentication();
app.UseAuthorization();

// 10. 映射控制器
app.MapControllers();

app.Run();
```

**📝 中间件顺序的重要性:**

```
正确顺序：
1. 异常处理      ← 捕获所有后续异常
2. 日志记录      ← 记录所有请求
3. CORS         ← 处理跨域
4. 路由         ← 匹配路由
5. 认证授权      ← 验证身份
6. 端点执行      ← Controller

❌ 错误顺序：
路由 → 异常处理 → Controller
（异常处理在路由之后，无法捕获路由错误）
```

---

## 📊 Step 6: 配置日志系统

修改 `appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=devices.db"
  }
}
```

**日志级别说明:**

```
Trace    → 最详细（开发调试）
Debug    → 调试信息
Information → 一般信息（默认）
Warning  → 警告信息
Error    → 错误信息
Critical → 严重错误
None     → 不记录
```

**在代码中使用日志:**

```csharp
public class DeviceService
{
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(ILogger<DeviceService> logger)
    {
        _logger = logger;
    }

    public async Task<Device> CreateDeviceAsync(CreateDeviceDto dto)
    {
        _logger.LogInformation("开始创建设备: {Name}", dto.Name);

        try
        {
            var device = await _repository.CreateAsync(dto);
            _logger.LogInformation("设备创建成功: ID={Id}", device.Id);
            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建设备失败: {Name}", dto.Name);
            throw;
        }
    }
}
```

---

## 🎯 Step 7: 创建健康检查端点

```csharp
// Program.cs 中添加
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();  // 检查数据库连接

// 在管道中添加
app.MapHealthChecks("/health");
```

**访问 `/health`:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456"
}
```

---

## 🎨 实战示例：完整的错误处理流程

### 1. Service层抛出业务异常

```csharp
public async Task<Device> UpdateDeviceAsync(int id, UpdateDeviceDto dto)
{
    var device = await _repository.GetByIdAsync(id);
    
    if (device == null)
        throw new NotFoundException($"设备ID {id} 不存在");

    if (dto.IpAddress != null && dto.IpAddress != device.IpAddress)
    {
        if (await _repository.ExistsByIpAsync(dto.IpAddress))
        {
            throw new ValidationException(
                "IP地址已被使用",
                new Dictionary<string, string[]>
                {
                    ["ipAddress"] = new[] { $"IP地址 {dto.IpAddress} 已被其他设备使用" }
                });
        }
    }

    // 更新逻辑...
    return device;
}
```

### 2. Controller简洁处理

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
{
    // 不需要try-catch，异常会被中间件处理
    var device = await _deviceService.UpdateDeviceAsync(id, dto);
    return Ok(device);
}
```

### 3. 前端收到标准化错误

```json
// 404错误
{
  "statusCode": 404,
  "message": "设备ID 999 不存在"
}

// 400验证错误
{
  "statusCode": 400,
  "message": "IP地址已被使用",
  "errors": {
    "ipAddress": ["IP地址 192.168.1.100 已被其他设备使用"]
  }
}
```

---

## 📝 今日总结

### ✅ 你学会了：

- [x] 中间件的概念和工作原理
- [x] 全局异常处理
- [x] 请求日志记录
- [x] 性能监控
- [x] CORS配置
- [x] 中间件管道的顺序
- [x] 日志系统的使用

### 🔑 中间件 vs Express.js对比：

| ASP.NET Core             | Express.js      | 说明    |
|--------------------------|-----------------|-------|
| `app.UseMiddleware<T>()` | `app.use(fn)`   | 注册中间件 |
| `await next()`           | `next()`        | 调用下一个 |
| `HttpContext`            | `req, res`      | 请求上下文 |
| `ILogger`                | `console.log`   | 日志记录  |
| `ExceptionMiddleware`    | `error handler` | 异常处理  |

---

## 🎯 明日预告：Day 8 - MQTT协议入门

明天开始进入物联网协议！你将学习：

- MQTT协议原理
- 发布/订阅模式
- MQTT客户端实现
- 设备消息通信

---

## 💾 作业

1. 实现一个API访问频率限制中间件（Rate Limiting）
2. 添加请求响应时间统计
3. 实现操作审计日志（谁在什么时间做了什么操作）
4. 思考：中间件和过滤器（Filter）的区别是什么？

---

**基础架构完成！明天开始IoT实战！🚀**


