using MultiTenantAPI.Services.CurrentTenant;
using Serilog.Context;

namespace MultiTenantAPI.Middlewares
{
    public class SerilogTenantEnricherMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogTenantEnricherMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ICurrentTenantService tenantService)
        {
            var tenantId = tenantService.TenantId ?? "unknown";

            using (LogContext.PushProperty("TenantId", tenantId))
            {
                await _next(context);
            }
        }
    }


}