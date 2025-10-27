//性能监控中间件

using System.Diagnostics;

namespace Day7MiddlewareAPI.Middleware;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;
    
    private readonly int _warningThresholdMs = 1000; // 1秒
    
    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await _next(context);
        
        stopwatch.Stop();
        
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        
        if (elapsedMs > _warningThresholdMs)
        {
            _logger.LogWarning("请求处理时间过长: {Method} {Path} 耗时 {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);
        }
        else
        {
            _logger.LogInformation("请求处理完成: {Method} {Path} 耗时 {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);
        }
    }
    
    
}