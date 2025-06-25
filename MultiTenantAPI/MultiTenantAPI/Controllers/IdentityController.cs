using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.IdentityService;


namespace MultiTenantAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class IdentityController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITenantService _tenantService;

        public IdentityController(IAuthService authService, ITenantService tenantService)
        {
            _authService = authService;
            _tenantService = tenantService;
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto dto)
        {
            var result = await _authService.CreateUser(dto);
            if (!result.Success)
            {
                if (result.Errors != null)
                    return BadRequest(new { result.ErrorMessage, result.Errors });

                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.Data);
        }




        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
        {
            var result = await _authService.SignIn(dto);
            if (!result.Success)
            {
                if (result.Errors != null)
                    return BadRequest(new { result.ErrorMessage, result.Errors });

                return BadRequest(result.ErrorMessage);
            }

            return Ok(new { token = result.Data });
        }

        [AllowAnonymous]
        [HttpPost("tenant-create")]
        public async Task<IActionResult> CreateTenant([FromBody] TenantDto dto)
        {
            var result = await _tenantService.CreateTenantAsync(dto);
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
