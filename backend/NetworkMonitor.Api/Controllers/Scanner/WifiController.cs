// File: Controllers/Scanner/WifiController.cs
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Services.Scanner;

namespace NetworkMonitor.Api.Controllers.Scanner;

[ApiController]
[Route("api/scanner/[controller]")]
public class WifiController : ControllerBase
{
    private readonly IWifiService _wifiService;

    public WifiController(IWifiService wifiService)
    {
        _wifiService = wifiService;
    }

    [HttpGet]
    public async Task<ActionResult<WifiInfoDto>> Get()
    {
        var info = await _wifiService.GetCurrentWifiAsync();
        return Ok(info);
    }
}