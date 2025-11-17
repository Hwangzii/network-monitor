using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Api.Controllers.System;

[ApiController]
[Route("api/system")]
[Produces("application/json")]
public class LocalSystemController : ControllerBase
{
    [HttpGet("local")]
    public IActionResult Get() => Ok(new { Message = "Local system info - coming soon" });
}