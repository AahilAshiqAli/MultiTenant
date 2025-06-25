using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Response;
using System.Security.Claims;
using System.Text.Json;

namespace MultiTenantAPI.Services.AdminService
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<AppUser> _userManager;

        public AdminService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ServiceResult<List<JsonElement>>> GetAllLogsAsync(ClaimsPrincipal user)
        {
            var tenantId = user.Claims.FirstOrDefault(c => c.Type == "tenantID")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return ServiceResult<List<JsonElement>>.Fail("TenantID is null");

            try
            {
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logDirectory))
                    return ServiceResult<List<JsonElement>>.Fail("Logs directory does not exist.");

                var logFiles = Directory.GetFiles(logDirectory, "*.txt");
                var logEntries = new List<JsonElement>();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                foreach (var file in logFiles)
                {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    string? line;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            using var jsonDoc = JsonDocument.Parse(line);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("Properties", out var properties) &&
                                properties.TryGetProperty("TenantId", out var tenantIdProperty) &&
                                tenantIdProperty.GetString() == tenantId)
                            {
                                logEntries.Add(root.Clone());
                            }
                        }
                        catch (JsonException) { /* Skip malformed lines */ }
                    }
                }

                return ServiceResult<List<JsonElement>>.Ok(logEntries);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<JsonElement>>.Fail($"Failed to read logs: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<object>>> GetAllUsersAsync(ClaimsPrincipal user)
        {
            var tenantId = user.Claims.FirstOrDefault(c => c.Type == "tenantID")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return ServiceResult<List<object>>.Fail("Tenant ID is missing from claims.");

            try
            {
                var pendingUsers = await _userManager.Users
                    .Where(u => u.TenantID == tenantId && !u.isApproved)
                    .ToListAsync();

                var result = new List<object>();

                foreach (var u in pendingUsers)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    result.Add(new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.FullName,
                        u.Gender,
                        u.DOB,
                        Role = roles.FirstOrDefault() ?? "Unassigned"
                    });
                }

                return ServiceResult<List<object>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<object>>.Fail("An error occurred: " + ex.Message);
            }
        }

        public async Task<ServiceResult<string>> ApproveUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return ServiceResult<string>.Fail("User not found.");

                user.isApproved = true;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return ServiceResult<string>.Fail(result.Errors);

                return ServiceResult<string>.Ok("User approved.");
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Fail("An error occurred: " + ex.Message);
            }
        }
    }

}
