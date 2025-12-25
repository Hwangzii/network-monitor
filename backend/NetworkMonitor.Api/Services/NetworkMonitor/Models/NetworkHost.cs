// file: NetworkMonitor.Api/Services/NetworkMonitor/Models/NetworkHost.cs
using System.Collections.Generic;

namespace NetworkMonitor.Api.Services.NetworkMonitor.Models;

public class NetworkHost
{
    public string RemoteIp { get; set; } = "";
    public string? Domain { get; set; } = null;  // Bonus: có thể resolve sau

    // === THÊM MỚI ĐỂ HỖ TRỢ TÍNH TOÁN TRAFFIC ===
    public int RemotePort { get; set; } = 0;     // Để phân biệt protocol (HTTP, HTTPS, DNS...)
    public long Bytes { get; set; } = 0;         // Tổng bytes đã truyền qua connection này (cumulative)
}

public class NetworkHostSet : HashSet<string> { }  // Để unique IP/domain (giữ nguyên)