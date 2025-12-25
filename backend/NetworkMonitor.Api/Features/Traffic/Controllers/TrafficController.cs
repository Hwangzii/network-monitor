// FILE: NetworkMonitor.Api/Features/Traffic/Controllers/TrafficController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using NetworkMonitor.Api.Features.Traffic.Services;
using NetworkMonitor.Api.Services.Traffic; // TrafficService (cũ)

namespace NetworkMonitor.Api.Features.Traffic.Controllers;

[ApiController]
[Route("api/traffic")]
[Produces("application/json")]
public class TrafficController : ControllerBase
{
    private readonly TrafficService _summaryService;
    private readonly TrafficChartService _chartService;

    public TrafficController(
        TrafficService summaryService,
        TrafficChartService chartService)
    {
        _summaryService = summaryService;
        _chartService = chartService;
    }

    // ======================
    // REALTIME SUMMARY
    // GET /api/traffic/summary
    // ======================
    [HttpGet("summary")]
    public ActionResult<TrafficSummaryDto> GetSummary()
    {
        return Ok(_summaryService.GetSummary());
    }

    // ======================
    // HISTORY CHART
    // GET /api/traffic/chart?range=5m
    // ======================
    [HttpGet("chart")]
    public async Task<ActionResult<TrafficChartResponseDto>> GetChart(
        [FromQuery] string range = "5m")
    {
        var result = await _chartService.GetChartAsync(range);
        return Ok(result);
    }
}
