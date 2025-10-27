//请求日志中间件

namespace Day7MiddlewareAPI.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.Now;
        //记录请求信息
        _logger.LogInformation("收到请求: {Method} {Path} 来自 {IP} 时间 {Time}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress,
            DateTime.UtcNow);
        
        //调用下一个中间件
        await _next(context);
        
        
        // 记录响应信息
        var duration = DateTime.Now - startTime;
        
        //记录响应信息
        _logger.LogInformation("完成响应: {Method} {Path} {StatusCode} 耗时 {Duration}ms",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            duration.TotalMilliseconds);
    }
    
}