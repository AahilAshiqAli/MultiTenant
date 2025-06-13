using AuthECAPI.Models;

namespace AuthECAPI.Services.Blob
{
    public interface IBlobStorageService
    {
        public Task<string> CreateTenantContainerAsync(string tenantId, Tenant settings);

        Task<IEnumerable<string>> ListAllContainersAsync();

    }
}
