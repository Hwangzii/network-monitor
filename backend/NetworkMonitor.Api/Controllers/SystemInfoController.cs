using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Services;

namespace NetworkMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/system-info")]  // THAY ĐỔI: Route cố định với dấu gạch ngang để match yêu cầu
    public class SystemInfoController : ControllerBase
    {
        private readonly SystemInfoProvider _provider;

        public SystemInfoController(SystemInfoProvider provider)
        {
            _provider = provider;
        }

        [HttpGet]  // Giữ nguyên, vì route class đã chỉ định
        public ActionResult<SystemInfoDto> GetSystemInfo()
        {
            var info = _provider.GetSystemInfo();
            return Ok(info);
        }
    }
}