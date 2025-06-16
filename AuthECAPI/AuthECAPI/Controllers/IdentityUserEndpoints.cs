using AuthECAPI.DTO;
using AuthECAPI.Models;
using AuthECAPI.Services.Blob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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

        var tenant = await dbContext.Tenants.FindAsync(userRegistrationModel.TenantID);
        if (tenant == null)
        {
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
      var result = await userManager.CreateAsync(
          user,
          userRegistrationModel.Password);
      await userManager.AddToRoleAsync(user, userRegistrationModel.Role);

      if (result.Succeeded)
        return Results.Ok(result);
      else
        return Results.BadRequest(result);
    }

    [AllowAnonymous]
    private static async Task<IResult> SignIn(
        UserManager<AppUser> userManager,
            [FromBody] LoginDto loginModel,
            IOptions<AppSettings> appSettings)
    {
      var user = await userManager.FindByEmailAsync(loginModel.Email);
      if (user != null && await userManager.CheckPasswordAsync(user, loginModel.Password))
      {
        var roles = await userManager.GetRolesAsync(user);
        var signInKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(appSettings.Value.JWTSecret)
                        );
        ClaimsIdentity claims = new ClaimsIdentity(new Claim[]
        {
          new Claim("userID",user.Id.ToString()),
          new Claim("tenantID",user.TenantID.ToString()),
          new Claim("gender",user.Gender.ToString()),
          new Claim("age",(DateTime.Now.Year - user.DOB.Year).ToString()),
          new Claim(ClaimTypes.Role,roles.First()),
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
        return Results.Ok(new { token });
      }
      else
        return Results.BadRequest(new { message = "Username or password is incorrect." });
    }

        [AllowAnonymous]
        private static async Task<IResult> CreateTenant(
            [FromBody] TenantDto model,
            AppDbContext dbContext,
            IBlobServiceFactory blobFactory)
        {
            try
            {
                var blobStorageService = blobFactory.GetClient(model.Provider);

                // Get all existing container names from DB
                var existingContainers = await blobStorageService.ListAllContainersAsync();

                // Check if the requested container already exists (case-insensitive comparison)
                if (existingContainers.Any(name => name.Equals(model.Container, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.BadRequest($"Container name '{model.Container}' is already in use.");
                }

                // Create tenant and save to DB
                var tenant = new Tenant
                {
                    Name = model.Name,
                    Provider = model.Provider,
                    Container = model.Container,
                    EnableVersioning = model.EnableVersioning,
                    RetentionDays = model.RetentionDays,
                    DefaultBlobTier = model.DefaultBlobTier
                };

                // Create container in blob storage
                var containerName = await blobStorageService.CreateTenantContainerAsync(tenant.Name, tenant);

                if (containerName == null)
                {
                    return Results.BadRequest("Failed to create blob container.");
                }

                dbContext.Tenants.Add(tenant);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new { tenant.TenantID, tenant.Name });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

    }
}
