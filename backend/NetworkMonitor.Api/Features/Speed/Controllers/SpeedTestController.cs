// file: Features/Speed/Controllers/SpeedTestController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.Features.Speed.DTOs;
using NetworkMonitor.Api.Features.Speed.Services;
using System.Text.Json;

namespace NetworkMonitor.Api.Features.Speed.Controllers;

[ApiController]
[Route("api/speed")]
public class SpeedTestController : ControllerBase
{
    private readonly SpeedTestService _speedTestService;
    private readonly SpeedTestHistoryService _historyService;

    public SpeedTestController(SpeedTestService speedTestService, SpeedTestHistoryService historyService)
    {
        _speedTestService = speedTestService;
        _historyService = historyService; // <-- THÊM
    }

    [HttpGet("run")]
    public async Task RunTest()
    {
        // Sửa theo khuyến nghị của ASP0019: dùng indexer hoặc Append thay vì Add
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        // Hoặc nếu muốn append nhiều giá trị cho cùng một header (hiếm dùng ở đây):
        // Response.Headers.Append("Cache-Control", "no-cache");

        await _speedTestService.ExecuteSpeedTestStreamingAsync(async (type, value) =>
        {
            var message = new { type, data = value };
            var json = JsonSerializer.Serialize(message);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        });
    }

    // === API MỚI: LẤY LỊCH SỬ ===
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
    {
        if (limit < 1) limit = 20;
        if (limit > 100) limit = 100;

        var history = await _historyService.GetHistoryAsync(limit);
        var total = await _historyService.GetTotalCountAsync();

        var response = new SpeedTestHistoryResponseDto
        {
            Success = true,
            Total = total,
            Data = history
        };

        return Ok(response);
    }
}