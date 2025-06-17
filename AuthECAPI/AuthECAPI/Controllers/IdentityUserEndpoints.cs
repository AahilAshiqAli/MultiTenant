using AuthECAPI.DTO;
using AuthECAPI.Models;
using AuthECAPI.Services.Blob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthECAPI.Controllers
{
 
public static class IdentityUserEndpoints
    {
        public static IEndpointRouteBuilder MapIdentityUserEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/signup", CreateUser);
            app.MapPost("/signin", SignIn);
            app.MapPost("/tenant-create", CreateTenant);
            return app;
        }

        [AllowAnonymous]
        private static async Task<IResult> CreateUser(
            UserManager<AppUser> userManager,
            [FromBody] UserRegistrationDto userRegistrationModel,
            AppDbContext dbContext)
        {
            Log.Information("CreateUser called with Email: {Email}, TenantID: {TenantID}", userRegistrationModel.Email, userRegistrationModel.TenantID);

            var tenant = await dbContext.Tenants.FindAsync(userRegistrationModel.TenantID);
            if (tenant == null)
            {
                Log.Warning("CreateUser failed: Invalid TenantID {TenantID}", userRegistrationModel.TenantID);
                return Results.BadRequest(new { message = "Invalid TenantID." });
            }

            AppUser user = new AppUser()
            {
                UserName = userRegistrationModel.Email,
                Email = userRegistrationModel.Email,
                FullName = userRegistrationModel.FullName,
                Gender = userRegistrationModel.Gender,
                DOB = DateOnly.FromDateTime(DateTime.Now.AddYears(-userRegistrationModel.Age)),
                LibraryID = userRegistrationModel.LibraryID,
                TenantID = userRegistrationModel.TenantID,
            };

            Log.Debug("Attempting to create user: {@User}", user);

            var result = await userManager.CreateAsync(user, userRegistrationModel.Password);
            Log.Information("UserManager.CreateAsync result: {@Result}", result);

            var roleResult = await userManager.AddToRoleAsync(user, userRegistrationModel.Role);
            Log.Information("UserManager.AddToRoleAsync result: {@RoleResult}", roleResult);

            if (result.Succeeded)
            {
                Log.Information("User created successfully: {UserId}", user.Id);
                return Results.Ok(result);
            }
            else
            {
                Log.Error("User creation failed: {@Errors}", result.Errors);
                return Results.BadRequest(result);
            }
        }

        [AllowAnonymous]
        private static async Task<IResult> SignIn(
            UserManager<AppUser> userManager,
            [FromBody] LoginDto loginModel,
            IOptions<AppSettings> appSettings)
        {
            Log.Information("SignIn called for Email: {Email}", loginModel.Email);

            var user = await userManager.FindByEmailAsync(loginModel.Email);
            if (user != null)
            {
                Log.Debug("User found: {UserId}", user.Id);
                var passwordValid = await userManager.CheckPasswordAsync(user, loginModel.Password);
                Log.Debug("Password check result: {PasswordValid}", passwordValid);

                if (passwordValid)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    Log.Debug("User roles: {@Roles}", roles);

                    var signInKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(appSettings.Value.JWTSecret)
                    );

                    ClaimsIdentity claims = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("userID", user.Id.ToString()),
                        new Claim("tenantID", user.TenantID.ToString()),
                        new Claim("gender", user.Gender.ToString()),
                        new Claim("age", (DateTime.Now.Year - user.DOB.Year).ToString()),
                        new Claim(ClaimTypes.Role, roles.First()),
                    });
                    if (user.LibraryID != null)
                        claims.AddClaim(new Claim("libraryID", user.LibraryID.ToString()!));

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = claims,
                        Expires = DateTime.UtcNow.AddDays(1),
                        SigningCredentials = new SigningCredentials(
                            signInKey,
                            SecurityAlgorithms.HmacSha256Signature
                        )
                    };
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                    var token = tokenHandler.WriteToken(securityToken);

                    Log.Information("JWT token generated for user: {UserId}", user.Id);
                    return Results.Ok(new { token });
                }
                else
                {
                    Log.Warning("SignIn failed: Incorrect password for Email: {Email}", loginModel.Email);
                }
            }
            else
            {
                Log.Warning("SignIn failed: User not found for Email: {Email}", loginModel.Email);
            }
            return Results.BadRequest(new { message = "Username or password is incorrect." });
        }

        [AllowAnonymous]
        private static async Task<IResult> CreateTenant(
            [FromBody] TenantDto model,
            AppDbContext dbContext,
            IBlobServiceFactory blobFactory,
            UserManager<AppUser> userManager)
        {
            Log.Information("CreateTenant called for Tenant Name: {TenantName}, Container: {Container}, Provider: {Provider}", model.Name, model.Container, model.Provider);
            try
            {
                var blobStorageService = blobFactory.GetClient(model.Provider);
                Log.Debug("Blob storage service client obtained for provider: {Provider}", model.Provider);

                var existingContainers = await blobStorageService.ListAllContainersAsync();
                Log.Debug("Existing containers: {@Containers}", existingContainers);

                if (existingContainers.Any(name => name.Equals(model.Container, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Warning("CreateTenant failed: Container name '{Container}' already in use.", model.Container);
                    return Results.BadRequest($"Container name '{model.Container}' is already in use.");
                }

                var tenant = new Tenant
                {
                    Name = model.Name,
                    Provider = model.Provider,
                    Container = model.Container,
                    EnableVersioning = model.EnableVersioning,
                    RetentionDays = model.RetentionDays,
                    DefaultBlobTier = model.DefaultBlobTier
                };

                Log.Debug("Attempting to create blob container for tenant: {@Tenant}", tenant);
                var containerName = await blobStorageService.CreateTenantContainerAsync(tenant.Name, tenant);

                if (containerName == null)
                {
                    Log.Error("Failed to create blob container for tenant: {TenantName}", tenant.Name);
                    return Results.BadRequest("Failed to create blob container.");
                }

                dbContext.Tenants.Add(tenant);
                await dbContext.SaveChangesAsync();
                Log.Information("Tenant created and saved to DB: {TenantID}", tenant.TenantID);

                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Gender = model.Gender,
                    DOB = DateOnly.FromDateTime(DateTime.Now.AddYears(-model.Age)),
                    LibraryID = model.LibraryID,
                    TenantID = tenant.TenantID
                };

                Log.Debug("Attempting to create admin user for tenant: {@User}", user);
                var userResult = await userManager.CreateAsync(user, model.Password);
                if (!userResult.Succeeded)
                {
                    Log.Error("Admin user creation failed: {@Errors}", userResult.Errors);
                    return Results.BadRequest(new { message = "User creation failed", errors = userResult.Errors });
                }

                var addRoleResult = await userManager.AddToRoleAsync(user, "Admin");
                Log.Information("Admin user created and assigned role: {UserId}, RoleResult: {@AddRoleResult}", user.Id, addRoleResult);

                Log.Information("Tenant and admin user created successfully: {TenantID}, {UserId}", tenant.TenantID, user.Id);
                return Results.Ok(new
                {   
                    tenant.TenantID,
                    tenant.Name,
                    AdminUser = new { user.Id, user.Email, user.FullName }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred in CreateTenant: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        }
    }
}
