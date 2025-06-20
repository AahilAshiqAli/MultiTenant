using MultiTenantAPI.Services.ProgressStore;
using Microsoft.AspNetCore.Mvc;

public static class SimpleProgressStore
{
    public static Dictionary<string, int> ProgressMap = new();
}

namespace MultiTenantAPI.Controllers
{
    [ApiController]
    [Route("api/progress")]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressStore _progressStore;

        public ProgressController(IProgressStore progressStore)
        {
            _progressStore = progressStore;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var userId = User.FindFirst("userID")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var progress = _progressStore.GetProgress(userId);
            return Ok(new { progress });
        }
    }
}
