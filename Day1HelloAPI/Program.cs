//创建应用构建器（准备配置你的应用）
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//添加控制器支持（告诉应用我们要用Controller处理请求）
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
//添加Swagger支持（自动生成API文档）
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//构建应用（完成配置，创建应用实例）
var app = builder.Build();
//启用Swagger（开发环境才开启）
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
//映射控制器路由（让API请求能找到对应的Controller）
app.MapControllers();
//运行应用（开始监听HTTP请求）
app.Run();
