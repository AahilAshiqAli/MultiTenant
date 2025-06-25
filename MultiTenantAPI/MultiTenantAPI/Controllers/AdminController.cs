using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantAPI.Services.AdminService;

namespace MultiTenantAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetAllLogs()
        {
            var result = await _adminService.GetAllLogsAsync(User);
            return result.Success ? Ok(result.Data) : BadRequest(new { result.ErrorMessage, result.Errors });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _adminService.GetAllUsersAsync(User);
            return result.Success ? Ok(result.Data) : BadRequest(new { result.ErrorMessage, result.Errors });
        }

        [HttpPost("approve-user/{userId}")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var result = await _adminService.ApproveUserAsync(userId);
            return result.Success ? Ok(new { message = result.Data }) : BadRequest(new { result.ErrorMessage, result.Errors });
        }

        [HttpGet("only")]
        public IActionResult AdminOnly() => Ok("Look For Logs");
    }
}
