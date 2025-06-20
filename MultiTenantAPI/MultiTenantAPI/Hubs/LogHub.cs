using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MultiTenantAPI.Hubs
{
    public class LogHub : Hub
    {
        private readonly ILogger<LogHub> _logger;

        public LogHub(ILogger<LogHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = Context.User?.FindFirst("tenantID")?.Value;

            if (!string.IsNullOrEmpty(tenantId))
            {


                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);

                await Clients.Group(tenantId).SendAsync("ReceiveLog", new
                {
                    Message = $"✅ Welcome, tenant {tenantId}",
                    Time = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("❌ Tenant ID not found in JWT claims during SignalR connection.");
                Console.WriteLine("❌ Tenant ID not found in JWT claims.");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = Context.User?.FindFirst("tenantID")?.Value;

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
