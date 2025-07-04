﻿using MultiTenantAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MultiTenantAPI.Extensions
{
    public static class EFCoreExtensions
    {
        public static IServiceCollection InjectDbContext(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                     options.UseSqlServer(config.GetConnectionString("DevDB")));
            services.AddDbContext<TenantDbContext>(options =>
                     options.UseSqlServer(config.GetConnectionString("DevDB")));
            return services;
        }
    } 
}
