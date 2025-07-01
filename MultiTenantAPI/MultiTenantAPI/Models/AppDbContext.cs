using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using MultiTenantAPI.Services.CurrentTenant;
using System.Reflection.Emit;

namespace MultiTenantAPI.Models
{
    public class AppDbContext:IdentityDbContext<AppUser>
    {
        private readonly ICurrentTenantService _currentTenantService;
        public Guid? CurrentTenantId { get; set; }
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

            builder.Entity<Content>()
                .HasIndex(c => c.TenantID)
                .HasDatabaseName("IX_Content_TenantId");

            if (CurrentTenantId.HasValue)
            {
                Guid tenantGuid = CurrentTenantId.Value;
                builder.Entity<Content>().HasQueryFilter(a => a.TenantID == tenantGuid);
            }

            builder.Entity<Content>()
                .HasOne(c => c.Tenant)
                .WithMany() // or .WithMany(t => t.Contents) if you have a collection
                .HasForeignKey(c => c.TenantID)
                .OnDelete(DeleteBehavior.Restrict); // prevents cascade delete

            // configure UserId if needed
            builder.Entity<Content>()
                .HasOne(c => c.AppUser)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Keep cascade here if needed
        }

        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ChangeLog &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (ChangeLog)entry.Entity;

                // Set tenant ID
                if (CurrentTenantId.HasValue)
                {
                    entity.TenantID = CurrentTenantId.Value; // Fix: Use .Value to extract Guid from nullable Guid
                }

                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                }

                entity.UpdatedAt = now;
            }
        }

        private async Task DeleteOrphanedTenantsAsync()
        {
            var deletedUsers = ChangeTracker.Entries<AppUser>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(e => e.Entity.TenantID)
                .Distinct()
                .ToList();

            foreach (var tenantId in deletedUsers)
            {
                // Check if there are any users for this tenant that are NOT being deleted
                bool anyUsersLeft = ChangeTracker.Entries<AppUser>()
                    .Any(e => e.Entity.TenantID == tenantId && e.State != EntityState.Deleted);

                if (!anyUsersLeft)
                {
                    var tenant = await Tenants.FindAsync(tenantId);
                    if (tenant != null)
                    {
                        Tenants.Remove(tenant);
                    }
                }
            }
        }

        public override int SaveChanges()
        {
            ApplyAudit();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();
            await DeleteOrphanedTenantsAsync();
            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}
