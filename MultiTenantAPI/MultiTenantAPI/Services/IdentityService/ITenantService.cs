using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.Response;

namespace MultiTenantAPI.Services.IdentityService
{
    public interface ITenantService
    {
        Task<ServiceResult<object>> CreateTenantAsync(TenantDto dto);
    }
}
