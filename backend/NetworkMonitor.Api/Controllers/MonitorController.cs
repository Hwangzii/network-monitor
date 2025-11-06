using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using System.Threading;

namespace NetworkMonitor.Api.Controllers
{
    [ApiController]
    [Route("monitor")]
    public class MonitorController : ControllerBase
    {
        private static bool _isMonitoring = false;
        private static long _bytesSent = 0;
        private static long _bytesReceived = 0;
        private static Thread? _monitorThread;
        private static string _localIP = "127.0.0.1";

        // Bắt đầu giám sát
        [HttpPost("start")]
        public IActionResult Start()
        {
            if (_isMonitoring)
                return Ok(new { message = "Monitoring is already running." });

            _isMonitoring = true;
            _monitorThread = new Thread(MonitorNetwork);
            _monitorThread.Start();

            return Ok(new { message = "Network monitoring started." });
        }

        // Dừng giám sát
        [HttpPost("stop")]
        public IActionResult Stop()
        {
            if (!_isMonitoring)
                return Ok(new { message = "Monitoring is not running." });

            _isMonitoring = false;
            _monitorThread?.Join();

            return Ok(new { message = "Network monitoring stopped." });
        }

        // Lấy trạng thái hiện tại
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new
            {
                ip = _localIP,
                bytesSent = _bytesSent,
                bytesReceived = _bytesReceived,
                bandwidthKBps = (_bytesSent + _bytesReceived) / 1024.0,
                isMonitoring = _isMonitoring
            });
        }

        // Hàm chạy giám sát nền (demo)
        private static void MonitorNetwork()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            _localIP = interfaces.FirstOrDefault()?.Name ?? "Unknown";

            while (_isMonitoring)
            {
                foreach (var ni in interfaces)
                {
                    var stats = ni.GetIPv4Statistics();
                    _bytesSent = stats.BytesSent;
                    _bytesReceived = stats.BytesReceived;
                }

                Console.WriteLine($"[Monitor] Sent: {_bytesSent} bytes | Received: {_bytesReceived} bytes");
                Thread.Sleep(2000);
            }
        }
    }
}
