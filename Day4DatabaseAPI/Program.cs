using Microsoft.EntityFrameworkCore;
using Day4DatabaseAPI.Data;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 添加DbContext服务（使用绝对路径，避免相对路径混淆）
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "devices.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    
    app.UseSwagger(); // 启用 Swagger 文档生成
    app.UseSwaggerUI(); // 启用 Swagger UI 界面（默认地址：/swagger）
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 确保自动迁移应用到数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
