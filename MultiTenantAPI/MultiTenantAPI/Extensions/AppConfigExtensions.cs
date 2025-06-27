using MultiTenantAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MultiTenantAPI.Extensions
{
  public static class AppConfigExtensions
  {
        public static WebApplication ConfigureCORS(this WebApplication app, IConfiguration config)
        {
            app.UseCors("AllowAngularClient");
            return app;
        }

        public static IServiceCollection AddAppConfig(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<AppSettings>(config.GetSection("AppSettings"));

            // Define named CORS policy here
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularClient", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                           .AllowAnyHeader() // allow any  headers
                           .AllowAnyMethod() // allow any HTTP method (GET, POST, PUT, DELETE, etc.)
                           .AllowCredentials(); // Needed for SignalR
                });
            });

            return services;
        }

    }
}
