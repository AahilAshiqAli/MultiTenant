using Microsoft.AspNetCore.Authorization;

namespace MultiTenantAPI.Controllers
{
    using MultiTenantAPI.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System.Net.Http;
    using System.Net.NetworkInformation;
    using System.Text.Json;

    public static class AuthorizationDemoController
    {
        public static IEndpointRouteBuilder MapAuthorizationDemoEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/AdminOnly", AdminOnly);
            app.MapGet("/admin/logs", (Delegate)GetAllLogs);
            app.MapGet("admin/users", (Delegate)GetAllUsers);
            return app;
        }

        [Authorize(Roles = "Admin")]
        private static string AdminOnly() => "Look For Logs";

        [Authorize(Roles = "Admin")]
        private static async Task<IResult> GetAllLogs(HttpContext httpContext)
        {
            try
            {
                var targetTenantId = httpContext.User.Claims
            .FirstOrDefault(c => c.Type == "tenantID")?.Value;

                if (targetTenantId == null)
                {
                    return Results.BadRequest(new { message = "TenantID is null" });
                }
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logDirectory))
                    return Results.NotFound("Logs directory does not exist.");

                var logFiles = Directory.GetFiles(logDirectory, "*.txt");
                var logEntries = new List<JsonElement>();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

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
                                tenantIdProperty.GetString() == targetTenantId)
                            {
                                logEntries.Add(root.Clone());
                            }
                        }
                        catch (JsonException)
                        {
                            // skip malformed line
                        }
                    }
                }

                return Results.Ok(logEntries);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to read logs: {ex.Message}");
            }
        }

        [Authorize]
        private static async Task<IResult> GetAllUsers(HttpContext httpContext, UserManager<AppUser> userManager)
        {
            try
            {
                var tenantId = httpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "tenantID")?.Value;

                if (string.IsNullOrEmpty(tenantId))
                {
                    return Results.BadRequest(new { message = "Tenant ID is missing from claims." });
                }

                var users = await userManager.Users
                    .Where(u => u.TenantID == tenantId)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.FullName,
                        u.Gender
                    })
                    .ToListAsync();

                return Results.Ok(users);
            }
            catch (Exception ex)
            {
                return Results.Problem("An error occurred: " + ex.Message);
            }
        }

    }
}
