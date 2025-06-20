using MultiTenantAPI.Services.CurrentTenant;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace MultiTenantAPI.Models
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
                builder.Entity<Content>().HasQueryFilter(a => a.TenantID == CurrentTenantId);

            builder.Entity<Content>()
             .HasOne(c => c.Tenant)
             .WithMany() // or .WithMany(t => t.Contents) if you have a collection
             .HasForeignKey(c => c.TenantID)
             .OnDelete(DeleteBehavior.Restrict); // 👈 prevents cascade delete

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
                if (!string.IsNullOrEmpty(CurrentTenantId))
                {
                    entity.TenantID = CurrentTenantId;
                }

                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                }

                entity.UpdatedAt = now;
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
            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}
