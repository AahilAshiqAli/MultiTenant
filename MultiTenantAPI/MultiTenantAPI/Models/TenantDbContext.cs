using Microsoft.EntityFrameworkCore;

namespace MultiTenantAPI.Models
{
    public class TenantDbContext : DbContext // Ensure TenantDbContext inherits from DbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) // Correctly call the base constructor
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
    }
}
