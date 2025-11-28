// NetworkMonitor.Api/Controllers/Scanner/NetworkScannerController.cs

using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Services.Scanner; // Thêm using này

namespace NetworkMonitor.Api.Controllers.Scanner;

[ApiController]
[Route("api/scanner")]
[Produces("application/json")]
public class NetworkScannerController : ControllerBase
{
    private readonly INetworkScannerService _scannerService;

    // Injection service qua constructor
    public NetworkScannerController(INetworkScannerService scannerService)
    {
        _scannerService = scannerService;
    }

    [HttpGet("devices")]
    public async Task<ActionResult<IEnumerable<NetworkDeviceResponseDto>>> GetDevices()
    {
        // Gọi hàm ScanNetworkAsync từ Service
        var devices = await _scannerService.ScanNetworkAsync();
        return Ok(devices);
    }
}