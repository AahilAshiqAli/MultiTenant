using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Models;
using MultiTenantAPI.Services.Blob;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.Response;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MultiTenantAPI.Services.IdentityService
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly ILogger<AuthService> _logger;
        private readonly IContentService _contentService;

        public AuthService(UserManager<AppUser> userManager, IOptions<AppSettings> settings, AppDbContext context, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _appSettings = settings.Value;
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<object>> CreateUser(UserRegistrationDto model)
        {
            var tenant = await _context.Tenants.FindAsync(model.TenantID);
            if (tenant == null)
            {
                var errors = new List<IdentityError>
                {
                    new IdentityError
                    {
                        Code = "InvalidTenantID",
                        Description = "Invalid TenantID"
                    }
                };

                return ServiceResult<object>.Fail(errors);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Gender = string.IsNullOrWhiteSpace(model.Gender) ? null
                    : char.ToUpper(model.Gender[0]) + model.Gender.Substring(1).ToLower(),
                    DOB = DateOnly.FromDateTime(DateTime.Now.AddYears(-model.Age)),
                    TenantID = model.TenantID.ToString(),
                    isApproved = false // Assuming new users are pending by default
                };
#pragma warning restore CS8601 // Possible null reference assignment.

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    if (createResult.Errors != null)
                    {
                        return ServiceResult<object>.Fail(createResult.Errors);
                    }
                    else
                    {
                        return ServiceResult<object>.Fail("what the fuck");
                    }
                }

                var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResult<object>
                    {
                        Success = false,
                        ErrorMessage = "Role assignment failed",
                        Errors = roleResult.Errors
                    };
                }

                await transaction.CommitAsync(); 

                return ServiceResult<object>.Ok(new { user.Id, user.Email });
            }
            catch (Exception ex)
            {
                Log.Information(ex, "error");
                await transaction.RollbackAsync();
                return new ServiceResult<object>
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during user creation.",
                    Errors = new { ex.Message }
                };
            }
        }


        public async Task<ServiceResult<object>> SignIn(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                Log.Debug("User found: {UserId}", user.Id);
                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                Log.Debug("Password check result: {PasswordValid}", passwordValid);

                if (passwordValid && user.isApproved)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    Log.Debug("User roles: {@Roles}", roles);

                    var signInKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_appSettings.JWTSecret)
                    );

                    ClaimsIdentity claims = new ClaimsIdentity(new Claim[]
                    {
                            new Claim("userID", user.Id.ToString()),
                            new Claim("tenantID", user.TenantID.ToString()),
                            new Claim("gender", user.Gender.ToString()),
                            new Claim("age", (DateTime.Now.Year - user.DOB.Year).ToString()),
                            new Claim(ClaimTypes.Role, roles.First()),
                    });


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

                    return ServiceResult<object>.Ok(token);
                }
                else
                {
                    if (!user.isApproved)
                    {
                        var error = new List<IdentityError>
                        {
                            new IdentityError
                            {
                                Code = "PendingUser",
                                Description = $"SignIn failed: Approval Remaining: {model.Email}"
                            }
                        };

                        return ServiceResult<object>.Fail(error);
                    }

                    var errors = new List<IdentityError>
                        {
                            new IdentityError
                            {
                                Code = "IncorrectPassword",
                                Description = $"SignIn failed: Incorrect password for Email: {model.Email}"
                            }
                        };

                    return ServiceResult<object>.Fail(errors);
                }
            }
            else
            {
                var errors = new List<IdentityError>
                    {
                        new IdentityError
                        {
                            Code = "UserNotFound",
                            Description = $"SignIn failed: User not found for Email: {model.Email}"
                        }
                    };

                return ServiceResult<object>.Fail(errors);
            }

        }

        public async Task<ServiceResult<object>> DeleteUser(string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

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
