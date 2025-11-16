using Microsoft.AspNetCore.Mvc;
namespace NetworkMonitor.Api.Controllers.Scanner;
[ApiController][Route("api/scanner")]public class NetworkScannerController : ControllerBase {[HttpGet("devices")]public IActionResult Get()=>Ok(new{});}}
