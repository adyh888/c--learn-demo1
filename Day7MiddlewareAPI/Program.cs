using Microsoft.EntityFrameworkCore;
using Day7MiddlewareAPI.Middleware;
using Day7MiddlewareAPI.Data;
using Day7MiddlewareAPI.Repositories.Interfaces;
using Day7MiddlewareAPI.Repositories.Implementations;
using Day7MiddlewareAPI.Services.Interfaces;
using Day7MiddlewareAPI.Services.Implementations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 添加DbContext服务（使用绝对路径，避免相对路径混淆）
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "devices.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

// 注册Repository（Scoped生命周期）
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

// 注册Service（Scoped生命周期）
builder.Services.AddScoped<IDevicesService, DeviceService>();

// 1. 添加 Swagger 服务
builder.Services.AddEndpointsApiExplorer(); // 用于暴露 API 元数据
builder.Services.AddSwaggerGen(); // 生成 Swagger 文档

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

// 添加自定义CORS
builder.Services.AddCustomCors();

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


//----------------------------------------------------------

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
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger(); // 启用 Swagger 文档生成
    app.UseSwaggerUI(); // 启用 Swagger UI 界面（默认地址：/swagger）
}

// 8. 路由
app.UseRouting();

// 9. 认证和授权
app.UseAuthorization();
app.UseAuthorization();

// 10. 映射控制器端点
app.MapControllers();

// 确保自动迁移应用到数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
