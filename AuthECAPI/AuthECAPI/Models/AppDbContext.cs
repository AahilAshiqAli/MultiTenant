using AuthECAPI.Services.CurrentTenant;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace AuthECAPI.Models
{
    public class AppDbContext:IdentityDbContext<AppUser>
    {
        private readonly ICurrentTenantService _currentTenantService;
        public string CurrentTenantId { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService currentTenantService):base(options)
        {
            _currentTenantService = currentTenantService;
            CurrentTenantId = _currentTenantService.TenantId;
        }

        public DbSet<AppUser> AppUsers { get; set; }

        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<Content> Contents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            if (!string.IsNullOrWhiteSpace(CurrentTenantId))
                builder.Entity<Content>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        }

        public override int SaveChanges()
        {
            var tenantEntries = ChangeTracker
                .Entries<Tenant>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in tenantEntries)
            {

                if (Guid.TryParse(CurrentTenantId, out var tenantGuid))
                {
                    entry.Entity.TenantID = tenantGuid;
                }

            }

            return base.SaveChanges();
        }

    }
}
