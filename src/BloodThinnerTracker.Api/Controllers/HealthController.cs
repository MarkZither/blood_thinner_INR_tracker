using Microsoft.AspNetCore.Mvc;

namespace BloodThinnerTracker.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [Route("/health")]
        public IActionResult Health()
        {
            // TODO: Add DB connectivity and encryption status checks
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
    }
}
