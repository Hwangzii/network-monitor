using Microsoft.AspNetCore.Mvc;

namespace NetworkMonitor.Api.Controllers.Scanner;

[ApiController]
[Route("api/scanner")]
[Produces("application/json")]
public class NetworkScannerController : ControllerBase
{
    [HttpGet("devices")]
    public IActionResult Get() => Ok(new { Message = "Network devices - coming soon" });
}