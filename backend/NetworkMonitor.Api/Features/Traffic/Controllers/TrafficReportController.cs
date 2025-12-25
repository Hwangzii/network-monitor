// file: NetworkMonitor.Api/Features/Traffic/Controllers/TrafficReportController.cs
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
            try 
            {
                var pdfBytes = await _reportService.GenerateReportAsync(range);
                var fileName = $"TrafficReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xuất PDF", detail = ex.Message });
            }
        }
    }
}