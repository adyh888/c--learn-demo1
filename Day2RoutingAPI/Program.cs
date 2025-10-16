var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// 1. 添加 Swagger 服务
builder.Services.AddEndpointsApiExplorer(); // 用于暴露 API 元数据
builder.Services.AddSwaggerGen(); // 生成 Swagger 文档

//允许前端跨域
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});




// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // 启用 Swagger 文档生成
    app.UseSwaggerUI(); // 启用 Swagger UI 界面（默认地址：/swagger）
    // app.MapOpenApi();
}

// 启用静态文件
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors();  // 启用CORS

app.UseAuthorization();

app.MapControllers();

app.Run();
