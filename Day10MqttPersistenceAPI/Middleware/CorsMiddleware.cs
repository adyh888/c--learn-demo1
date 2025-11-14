namespace Day10MqttPersistenceAPI.Middleware;

public class CorsMiddleware
{
    
}


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