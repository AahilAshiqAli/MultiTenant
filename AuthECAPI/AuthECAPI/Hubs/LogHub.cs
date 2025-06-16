using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using AuthECAPI.Services.CurrentTenant;
using Microsoft.AspNetCore.Authorization;

namespace AuthECAPI.Hubs
{
    public class LogHub : Hub
    {
        private readonly ICurrentTenantService _tenantService;

        public LogHub(ICurrentTenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = _tenantService.TenantId;
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = _tenantService.TenantId;
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}