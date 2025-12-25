// file : NetworkMonitor.Api/Controllers/Firewall/AppsController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Services.Firewall;
using System.Runtime.Versioning;

namespace NetworkMonitor.Api.Controllers.Firewall;

[ApiController]
[Route("api/firewall")] // Route chung cho Firewall (ví dụ: api/firewall)
[Produces("application/json")]
[SupportedOSPlatform("windows")] // Chỉ chạy trên Windows
public class AppsController : ControllerBase
{
    private readonly IFirewallService _firewallService;

    // Khởi tạo Service qua Dependency Injection
    public AppsController(IFirewallService firewallService) 
        => _firewallService = firewallService;

    // ====================================================================
    // API 1: GET /api/firewall/apps (Danh sách ứng dụng)
    // ====================================================================
    /// <summary>
    /// Lấy danh sách các ứng dụng đang hoạt động hoặc đã gỡ cài đặt (API 1)
    /// </summary>
    [HttpGet("apps")]
    [ProducesResponseType(typeof(PagedResponseDto<FirewallAppDto>), 200)]
    public async Task<IActionResult> GetFirewallApps(
        [FromQuery] string status, 
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50) 
    {
        // 1. Kiểm tra đầu vào
        if (string.IsNullOrEmpty(status) || 
            (status.ToLower() != "active" && status.ToLower() != "uninstalled"))
        {
            return BadRequest(new { message = "Tham số 'status' phải là 'active' hoặc 'uninstalled'." });
        }
        
        // 2. Gọi Service Layer
        var response = await _firewallService.GetFirewallAppsAsync(
            status, 
            page, 
            limit 
        );

        return Ok(response);
    }

    // ====================================================================
    // API 2: GET /api/firewall/apps/{appId}/processes (Processes con)
    // ====================================================================
    /// <summary>
    /// Lấy danh sách Processes con đang hoạt động của một ứng dụng (API 2)
    /// </summary>
    [HttpGet("apps/{appId}/processes")]
    [ProducesResponseType(typeof(List<FirewallProcessDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAppProcesses([FromRoute] string appId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            return BadRequest(new { message = "Tham số appId không được để trống." });
        }
        
        // Gọi Service Layer
        var processes = await _firewallService.GetAppProcessesAsync(appId);

        if (processes == null || !processes.Any())
        {
            // Trả về 404 nếu không tìm thấy ứng dụng hoặc không có processes con
            return NotFound(new { message = $"Không tìm thấy Processes nào cho ứng dụng ID: {appId}." });
        }

        return Ok(processes);
    }
}