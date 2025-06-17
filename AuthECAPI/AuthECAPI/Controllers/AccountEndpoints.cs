using AuthECAPI.Models;
using AuthECAPI.Services.CurrentTenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;

namespace AuthECAPI.Controllers
{
        public static class AccountEndpoints
    {
        public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
        {
            Log.Information("Mapping account endpoints.");
            app.MapGet("/UserProfile", GetUserProfile);
            app.MapGet("/TenantUserCount", GetTenantUserCount);
            Log.Information("Mapped GET /UserProfile endpoint.");

            return app;
        }

        [Authorize]
        private static async Task<IResult> GetUserProfile(
          ClaimsPrincipal user,
          UserManager<AppUser> userManager)
        {
            Log.Information("GetUserProfile called.");

            try
            {
                if (user == null)
                {
                    Log.Warning("ClaimsPrincipal user is null.");
                    return Results.BadRequest("User context is missing.");
                }

                var userIdClaim = user.Claims.FirstOrDefault(x => x.Type == "userID");
                if (userIdClaim == null)
                {
                    Log.Warning("userID claim not found in ClaimsPrincipal.");
                    return Results.BadRequest("User ID claim missing.");
                }

                string userID = userIdClaim.Value;
                Log.Information("Extracted userID: {UserID}", userID);

                var userDetails = await userManager.FindByIdAsync(userID);
                if (userDetails == null)
                {
                    Log.Warning("User not found for userID: {UserID}", userID);
                    return Results.NotFound("User not found.");
                }

                Log.Information("User found: {Email}, {FullName}", userDetails.Email, userDetails.FullName);

                return Results.Ok(
                  new
                  {
                      Email = userDetails.Email,
                      FullName = userDetails.FullName,
                  });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred in GetUserProfile.");
                return Results.Problem("An error occurred while retrieving the user profile.");
            }
        }

        [Authorize]
        private static async Task<IResult> GetTenantUserCount(
        ICurrentTenantService tenantService,
        AppDbContext dbContext)
         {
                Log.Information("GetTenantUserCount called.");

                var tenantId = tenantService.TenantId;
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    Log.Warning("TenantId is missing in ICurrentTenantService.");
                    return Results.BadRequest("TenantId is missing.");
                }

                var userCount = await dbContext.AppUsers
                    .CountAsync(u => u.TenantID.ToString() == tenantId);

                Log.Information("Tenant {TenantId} has {UserCount} users.", tenantId, userCount);

                return Results.Ok(new { TenantId = tenantId, UserCount = userCount });
         }
    }
}
