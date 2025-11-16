using Microsoft.AspNetCore.Mvc;
namespace NetworkMonitor.Api.Controllers.Traffic;
[ApiController][Route("api/traffic")]public class TrafficController : ControllerBase {[HttpGet("summary")]public IActionResult Get()=>Ok(new{});}}
