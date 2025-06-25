using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.Response;

namespace MultiTenantAPI.Services.IdentityService
{
    public interface IAuthService
    {
        Task<ServiceResult<object>> CreateUser(UserRegistrationDto dto);
        Task<ServiceResult<object>> SignIn(LoginDto dto);
    }
}
