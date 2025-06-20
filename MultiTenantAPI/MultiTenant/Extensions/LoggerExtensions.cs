using MultiTenantAPI.Hubs;
using MultiTenantAPI.Logging;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Configuration;
using System;

namespace MultiTenantAPI.Extensions
{
    public static class LoggerExtensions
    {
        public static LoggerConfiguration WithSignalRSink(
            this LoggerConfiguration loggerConfiguration,
            IServiceProvider services,
            IFormatProvider? formatProvider = null)
        {
            return loggerConfiguration.WriteTo.Sink(new SignalRSink(services, formatProvider));
        }
    }
}
