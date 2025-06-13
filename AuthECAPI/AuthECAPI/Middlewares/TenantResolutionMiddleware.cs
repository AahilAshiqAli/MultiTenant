using AuthECAPI.Services.CurrentTenant;

namespace AuthECAPI.Middlewares
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentTenantService currentTenantService)
        {  
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var tenantId = user.Claims.FirstOrDefault(c => c.Type == "tenantID")?.Value;

                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    await currentTenantService.SetTenant(tenantId);
                }
            }

            await _next(context); // ➜ move to next middleware or controller
        }
    }
}
