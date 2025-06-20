using MultiTenantAPI.Models;

namespace MultiTenantAPI.Services.Blob
{
    public interface IBlobStorageService
    {
        public Task<string> CreateTenantContainerAsync(string containerName, Tenant settings);

        Task<IEnumerable<string>> ListAllContainersAsync();

        public Task<bool> DeleteContainerAsync(string tenantId);

    }
}
