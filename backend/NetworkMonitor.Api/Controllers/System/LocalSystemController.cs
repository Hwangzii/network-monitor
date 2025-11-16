using Microsoft.AspNetCore.Mvc;
namespace NetworkMonitor.Api.Controllers.System;
[ApiController][Route("api/system")]public class LocalSystemController : ControllerBase {[HttpGet("local")]public IActionResult Get()=>Ok(new{});}}
