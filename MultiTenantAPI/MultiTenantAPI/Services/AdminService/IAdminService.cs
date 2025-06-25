using MultiTenantAPI.Services.Response;
using System.Security.Claims;
using System.Text.Json;

namespace MultiTenantAPI.Services.AdminService
{
    public interface IAdminService
    {
        Task<ServiceResult<List<JsonElement>>> GetAllLogsAsync(ClaimsPrincipal user);
        Task<ServiceResult<List<object>>> GetAllUsersAsync(ClaimsPrincipal user);
        Task<ServiceResult<string>> ApproveUserAsync(string userId);

    }
}
