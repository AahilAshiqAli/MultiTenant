using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Blob;
using MultiTenantAPI.Services.Response;
using Serilog;

namespace MultiTenantAPI.Services.IdentityService
{
    public class TenantService : ITenantService
    {
        private readonly AppDbContext _context;
        private readonly IBlobServiceFactory _blobFactory;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<TenantService> _logger;

        public TenantService(AppDbContext dbContext, IBlobServiceFactory blobFactory, UserManager<AppUser> userManager, ILogger<TenantService> logger)
        {
            _context = dbContext;
            _blobFactory = blobFactory;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ServiceResult<object>> CreateTenantAsync(TenantDto model)
        {
            Log.Information("CreateTenant called for Tenant Name: {TenantName}, Container: {Container}, Provider: {Provider}", model.Name, model.Container, model.Provider);

            using var transaction = await _context.Database.BeginTransactionAsync();
            var blobStorageService = _blobFactory.GetClient(model.Provider);
            try
            {

                var existingContainers = await blobStorageService.ListAllContainersAsync();
                if (existingContainers.Any(name => name.Equals(model.Container, StringComparison.OrdinalIgnoreCase)))
                {
                    return ServiceResult<object>.Fail($"Container name '{model.Container}' is already in use.");
                }

                var tenant = new Tenant
                {
                    Name = model.Name,
                    Provider = model.Provider,
                    Container = $"tenant-{model.Container.ToLower()}",
                    EnableVersioning = model.EnableVersioning,
                    RetentionDays = model.RetentionDays,
                    DefaultBlobTier = model.DefaultBlobTier
                };

                var containerName = await blobStorageService.CreateTenantContainerAsync(tenant.Container, tenant);
                if (containerName == null)
                {
                    return ServiceResult<object>.Fail("Failed to create blob container.");
                }

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

#pragma warning disable CS8601 // Possible null reference assignment.
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Gender = string.IsNullOrWhiteSpace(model.Gender) ? null
                    : char.ToUpper(model.Gender[0]) + model.Gender.Substring(1).ToLower(),
                    DOB = DateOnly.FromDateTime(DateTime.Now.AddYears(-model.Age)),
                    TenantID = tenant.TenantID,
                    isApproved = true
                };
#pragma warning restore CS8601 // Possible null reference assignment.
                _logger.LogInformation("password: ${Password}", model.Password);
                var userResult = await _userManager.CreateAsync(user, model.Password);
                if (!userResult.Succeeded)
                {
                    // Compensate: Delete container and rollback DB
                    await blobStorageService.DeleteContainerAsync(tenant.Container);
                    return new ServiceResult<object>
                    {
                        Success = false,
                        ErrorMessage = "User creation failed",
                        Errors = userResult.Errors
                    };
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user); // Compensate user
                    await blobStorageService.DeleteContainerAsync(tenant.Container); // Compensate container
                    return new ServiceResult<object>
                    {
                        Success = false,
                        ErrorMessage = "Role assignment failed",
                        Errors = addRoleResult.Errors
                    };
                }

                await transaction.CommitAsync(); // Commit all DB actions

                return ServiceResult<object>.Ok(new
                {
                    tenant.TenantID,
                    tenant.Name,
                    AdminUser = new { user.Id, user.Email, user.FullName }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in CreateTenant");

                await transaction.RollbackAsync();


                try
                {
                    bool result = await blobStorageService.DeleteContainerAsync(model.Container);
                    if (!result)
                    {
                        throw new Exception();
                    }

                }
                catch
                {
                    _logger.LogWarning("Failed to delete container {Container} after exception in CreateTenant", model.Container);
                }

                return new ServiceResult<object>
                {
                    Success = false,
                    ErrorMessage = "Unhandled exception during tenant creation.",
                    Errors = new { ex.Message }
                };
            }

        }
    }
}
