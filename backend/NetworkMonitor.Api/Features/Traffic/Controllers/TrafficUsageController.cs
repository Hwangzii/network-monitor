// Features\Traffic\Controllers\TrafficUsageController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.Features.Traffic.Services;

namespace NetworkMonitor.Api.Features.Traffic.Controllers;

[ApiController]
[Route("api/traffic")]
public class TrafficUsageController : ControllerBase
{
    private readonly TrafficUsageService _usageService;

    public TrafficUsageController(TrafficUsageService usageService)
    {
        _usageService = usageService;
    }

    [HttpGet("usage-summary")]
    public IActionResult GetUsageSummary()
    {
        var summary = _usageService.GetCurrentUsageSummary();
        return Ok(summary);
    }
}