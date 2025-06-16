using AuthECAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Serilog.Core;
using Serilog.Events;

namespace AuthECAPI.Logging
{
    public class SignalRSink : ILogEventSink
    {
        private readonly IServiceProvider _services;
        private readonly IFormatProvider? _formatProvider;

        public SignalRSink(IServiceProvider services, IFormatProvider? formatProvider)
        {
            _services = services;
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                if (!logEvent.Properties.TryGetValue("TenantId", out var tenantProp))
                    return;

                var tenantId = tenantProp.ToString().Trim('"');

                var hubContext = _services.GetService(typeof(IHubContext<LogHub>)) as IHubContext<LogHub>;
                if (hubContext == null)
                    return;

                var message = new
                {
                    Timestamp = logEvent.Timestamp.ToString("o"),
                    Level = logEvent.Level.ToString(),
                    Message = logEvent.RenderMessage(_formatProvider),
                    Exception = logEvent.Exception?.ToString(),
                    TenantId = tenantId
                };

                // Fire-and-forget, do not await in logging sink
                _ = hubContext.Clients.Group(tenantId).SendAsync("ReceiveLog", message);
            }
            catch
            {
                Console.WriteLine("Error in SignalRSink Emit method. Ensure that the SignalR hub is properly configured and the tenant ID is valid.");
            }
        }
    }

}
