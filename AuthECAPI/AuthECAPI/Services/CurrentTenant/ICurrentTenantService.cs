namespace AuthECAPI.Services.CurrentTenant
{
    public interface ICurrentTenantService
    {  
        string? TenantId { get; }

        public Task<bool> SetTenant(string tenant);

    }
}
