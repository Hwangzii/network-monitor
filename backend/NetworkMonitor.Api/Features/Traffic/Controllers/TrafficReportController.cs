// NetworkMonitor.Api/Features/Traffic/Controllers/TrafficReportController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.Features.Traffic.Services;

namespace NetworkMonitor.Api.Features.Traffic.Controllers
{
    [ApiController]
    [Route("api/traffic/export")]
    public class TrafficReportController : ControllerBase
    {
        private readonly TrafficReportService _reportService;

        public TrafficReportController(TrafficReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("pdf")]
        public async Task<IActionResult> GetPdf([FromQuery] string range = "5m")
        {
            var pdfBytes = await _reportService.GenerateReportAsync(range);
            return File(pdfBytes, "application/pdf", "TrafficReport.pdf");
        }
    }
}
