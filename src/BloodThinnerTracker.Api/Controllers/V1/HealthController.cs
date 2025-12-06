using Microsoft.AspNetCore.Mvc;

namespace BloodThinnerTracker.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [Route("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
