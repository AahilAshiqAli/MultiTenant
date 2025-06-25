using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.Response;
using Serilog;
using System.Security.Claims;

namespace MultiTenantAPI.Services.AccountService
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentTenantService _tenantService;
        private readonly AppDbContext _dbContext;
        private readonly IContentService _contentService;
        private readonly IHttpContextAccessor _contextAccessor;

        public AccountService(
            UserManager<AppUser> userManager,
            ICurrentTenantService tenantService,
            AppDbContext dbContext,
            IContentService contentService,
            IHttpContextAccessor contextAccessor)
        {
            _userManager = userManager;
            _tenantService = tenantService;
            _dbContext = dbContext;
            _contextAccessor = contextAccessor;
            _contentService = contentService;
        }

        public async Task<ServiceResult<object>> GetUserProfileAsync(ClaimsPrincipal user)
        {
            try
            {
                var userIdClaim = user.Claims.FirstOrDefault(x => x.Type == "userID");
                if (userIdClaim == null)
                {
                    Log.Warning("userID claim not found.");
                    return ServiceResult<object>.Fail("User ID claim missing.");
                }

                var userId = userIdClaim.Value;
                var userDetails = await _userManager.FindByIdAsync(userId);
                if (userDetails == null)
                {
                    Log.Warning("User not found for userID: {UserId}", userId);
                    return ServiceResult<object>.Fail("User not found.");
                }

                return ServiceResult<object>.Ok(new
                {
                    userDetails.Email,
                    userDetails.FullName
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetUserProfileAsync.");
                return ServiceResult<object>.Fail("An error occurred while fetching user profile.");
            }
        }

        public async Task<ServiceResult<object>> GetTenantUserCountAsync()
        {
            var tenantId = _tenantService.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return ServiceResult<object>.Fail("TenantId is missing.");
            }

            try
            {
                var userCount = await _dbContext.AppUsers
                    .CountAsync(u => u.TenantID.ToString() == tenantId);

                return ServiceResult<object>.Ok(new
                {
                    TenantId = tenantId,
                    UserCount = userCount
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetTenantUserCountAsync.");
                return ServiceResult<object>.Fail("Error fetching tenant user count.");
            }
        }

        public async Task<ServiceResult<object>> GetTenantFileCountAsync()
        {
            var tenantId = _tenantService.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return ServiceResult<object>.Fail("TenantId is missing.");
            }

            try
            {
                var fileCount = await _dbContext.Contents
                    .CountAsync(c => c.TenantID == tenantId);

                return ServiceResult<object>.Ok(new
                {
                    TenantId = tenantId,
                    FileCount = fileCount
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetTenantFileCountAsync.");
                return ServiceResult<object>.Fail("Error fetching tenant file count.");
            }
        }

        public async Task<ServiceResult<object>> DeleteUser(string? userId)
        {
            if (userId == null)
                userId = _contextAccessor.HttpContext?.User?.FindFirstValue("userID");

            if (userId == null)
            {
                return ServiceResult<object>.Fail(new List<IdentityError>
                    {
                        new IdentityError
                        {
                            Code = "UserNotFound",
                            Description = "The specified user does not exist."
                        }
                        });
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<object>.Fail(new List<IdentityError>
                    {
                        new IdentityError
                        {
                            Code = "UserNotFound",
                            Description = "The specified user does not exist."
                        }
                        });
                }

                bool success = await _contentService.DeleteUserContentAsync(userId);

                if (!success)
                {
                    return ServiceResult<object>.Fail(new List<IdentityError>
                    {
                        new IdentityError
                        {
                            Code = "DeletionFailed",
                            Description = "Deletiojn from cloud failed."
                        }
                        });
                }


                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any())
                {
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, roles);
                    if (!removeRolesResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return ServiceResult<object>.Fail(removeRolesResult.Errors);
                    }
                }

                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<object>.Fail(deleteResult.Errors);
                }

                await transaction.CommitAsync();
                return ServiceResult<object>.Ok(new { userId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Error(ex, "Concurrency error during user deletion for userId: {UserId}", userId);
                // Handle the concurrency error
                return ServiceResult<object>.Fail(new List<IdentityError>
                {
                    new IdentityError
                    {
                        Code = "ConcurrencyFailure",
                        Description = "The user was changed by another process. Please try again."
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during user deletion");
                await transaction.RollbackAsync();
                return new ServiceResult<object>
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during user deletion.",
                    Errors = new { ex.Message }
                };
            }
        }

    }

}
