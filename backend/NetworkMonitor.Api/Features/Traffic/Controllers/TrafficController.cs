// NetworkMonitor.Api/Controllers/Traffic/TrafficController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Services.Traffic;

namespace NetworkMonitor.Api.Controllers.Traffic;

[ApiController]
[Route("api/traffic")]
[Produces("application/json")]
public class TrafficController : ControllerBase
{
    private readonly TrafficService _service;

    public TrafficController(TrafficService service) => _service = service;

    [HttpGet("summary")]
    public ActionResult<TrafficSummaryDto> GetSummary()
        => Ok(_service.GetSummary());
}