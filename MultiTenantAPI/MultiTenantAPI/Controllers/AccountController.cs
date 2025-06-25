using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantAPI.Services.AccountService;

namespace MultiTenantAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("UserProfile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var result = await _accountService.GetUserProfileAsync(User);
            return result.Success ? Ok(result.Data) : BadRequest(new { result.ErrorMessage, result.Errors });
        }

        [HttpGet("TenantUserCount")]
        public async Task<IActionResult> GetTenantUserCount()
        {
            var result = await _accountService.GetTenantUserCountAsync();
            return result.Success ? Ok(result.Data) : BadRequest(new { result.ErrorMessage, result.Errors });
        }

        [HttpGet("TenantFileCount")]
        public async Task<IActionResult> GetTenantFileCount()
        {
            var result = await _accountService.GetTenantFileCountAsync();
            return result.Success ? Ok(result.Data) : BadRequest(new { result.ErrorMessage, result.Errors });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromQuery] string? userId)
        {
            var result = await _accountService.DeleteUser(userId);
            if (!result.Success)
            {
                if (result.Errors != null)
                    return BadRequest(new { result.ErrorMessage, result.Errors });

                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }
    }
}
