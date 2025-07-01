namespace MultiTenantAPI.Services.CurrentTenant
{
    public interface ICurrentTenantService
    {  
        Guid? TenantId { get; }

        public Task<bool> SetTenant(string tenant);

    }
}
