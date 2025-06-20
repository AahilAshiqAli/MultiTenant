using MultiTenantAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MultiTenantAPI.Services.CurrentTenant
{
    public class CurrentTenantService : ICurrentTenantService
    {
        private readonly TenantDbContext _dbContext;
        private readonly ILogger<CurrentTenantService> _logger;

        public CurrentTenantService(TenantDbContext dbContext, ILogger<CurrentTenantService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public string? TenantId { get; private set; }

        public async Task<bool> SetTenant(string tenant)
        {
            //_logger.LogInformation("SetTenant called with tenant: {Tenant}", tenant);

            if (!Guid.TryParse(tenant, out var tenantGuid))
            {
                _logger.LogError("Invalid Tenant ID format: {TenantId}", tenant);
                throw new InvalidOperationException("Invalid Tenant ID format.");
            }

            //_logger.LogDebug("Looking up tenant with ID: {TenantGuid}", tenantGuid);

            var tenantInfo = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.TenantID == tenant);

            if (tenantInfo != null)
            {
                TenantId = tenantInfo.TenantID.ToString();
                //_logger.LogInformation("Tenant found and set: {TenantId}", TenantId);
                return true;
            }

            _logger.LogError("Tenant not found for ID: {TenantGuid}", tenant);
            throw new Exception("Tenant not found.");
        }
    }
}
