using AuthECAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthECAPI.Services.CurrentTenant
{
    public class CurrentTenantService : ICurrentTenantService
    {
        private readonly TenantDbContext _dbContext;

        public CurrentTenantService(TenantDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string? TenantId { get; private set; }

        public async Task<bool> SetTenant(string tenant)
        {
            if (!Guid.TryParse(tenant, out var tenantGuid))
            {
                throw new ArgumentException("Invalid Tenant ID format.");
            }

            var tenantInfo = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.TenantID == tenantGuid);

            if (tenantInfo != null)
            {
                TenantId = tenantInfo.TenantID.ToString();
                return true;
            }

            throw new Exception("Tenant not found.");


        }
    }
}
