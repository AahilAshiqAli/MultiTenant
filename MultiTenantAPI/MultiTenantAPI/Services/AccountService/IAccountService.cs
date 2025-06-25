using MultiTenantAPI.Services.Response;
using System.Security.Claims;

namespace MultiTenantAPI.Services.AccountService
{
    public interface IAccountService
    {
        Task<ServiceResult<object>> GetUserProfileAsync(ClaimsPrincipal user);
        Task<ServiceResult<object>> GetTenantUserCountAsync();
        Task<ServiceResult<object>> GetTenantFileCountAsync();
        Task<ServiceResult<object>> DeleteUser(string? userId);
    }
}
