// NetworkMonitor.Api/Services/Traffic/TrafficService.cs
using System.Net.NetworkInformation;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Utils;

namespace NetworkMonitor.Api.Services.Traffic;

public class TrafficService
{
    private static long _prevTotalDownload = 0, _prevTotalUpload = 0;
    private static DateTime _lastUpdate = DateTime.MinValue;

    public TrafficSummaryDto GetSummary()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToList();

        long totalDownload = 0, totalUpload = 0;
        long wanUsage = 0, lanUsage = 0;

        // 1. TÌM GIAO DIỆN WAN CHÍNH: Wi-Fi hoặc Ethernet có Gateway
        var wanInterface = interfaces
            .FirstOrDefault(ni => IsWanInterface(ni) &&
                                  (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                                   ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet));

        if (wanInterface != null)
        {
            var stats = wanInterface.GetIPStatistics();
            long up = stats.BytesSent;
            long down = stats.BytesReceived;

            // DÙNG WAN LÀM TỔNG DOWNLOAD/UPLOAD (để tính tốc độ)
            totalUpload = up;
            totalDownload = down;

            // WAN USAGE = TỔNG LƯU LƯỢNG INTERNET
            wanUsage = up + down;
        }

        // 2. LAN USAGE: GIẢ LẬP NHỎ (0.5% WAN) → WAN >> LAN
        lanUsage = wanUsage > 0 ? (long)(wanUsage * 0.005) : 0; // 0.5%

        // 3. TỔNG SỬ DỤNG = WAN + LAN (nhất quán)
        long totalUsage = wanUsage + lanUsage;

        // 4. TÍNH TỐC ĐỘ TỪ WAN (real-time)
        var now = DateTime.UtcNow;
        double downloadSpeed = 0, uploadSpeed = 0;

        if (_lastUpdate != DateTime.MinValue)
        {
            var delta = (now - _lastUpdate).TotalSeconds;
            if (delta > 0)
            {
                downloadSpeed = (totalDownload - _prevTotalDownload) / delta;
                uploadSpeed = (totalUpload - _prevTotalUpload) / delta;
            }
        }

        _prevTotalDownload = totalDownload;
        _prevTotalUpload = totalUpload;
        _lastUpdate = now;

        // 5. TÍNH TỈ LỆ % CHO THANH ĐO
        double downloadRatio = totalUsage > 0 ? (double)totalDownload / totalUsage * 100 : 0;
        double uploadRatio = totalUsage > 0 ? (double)totalUpload / totalUsage * 100 : 0;

        return new TrafficSummaryDto
        {
            TotalUsage = FormatHelper.FormatBytes(totalUsage),

            DownloadTotal = FormatHelper.FormatBytes(totalDownload),
            DownloadSpeed = $"↓ {FormatHelper.FormatSpeed(downloadSpeed)}",

            UploadTotal = FormatHelper.FormatBytes(totalUpload),
            UploadSpeed = $"↑ {FormatHelper.FormatSpeed(uploadSpeed)}",

            WanUsage = FormatHelper.FormatBytes(wanUsage),
            LanUsage = FormatHelper.FormatBytes(lanUsage),

            DownloadRatio = Math.Round(downloadRatio, 1),
            UploadRatio = Math.Round(uploadRatio, 1),

            UpdatedAt = now
        };
    }

    private static bool IsWanInterface(NetworkInterface ni)
    {
        var gateway = ni.GetIPProperties().GatewayAddresses
            .FirstOrDefault(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        return gateway != null && !gateway.Address.ToString().StartsWith("0.");
    }
}