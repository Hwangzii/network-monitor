using NetworkMonitor.Api.Services.NetworkMonitor.Models;
using System.Collections.Generic;

namespace NetworkMonitor.Api.Services.NetworkMonitor;

public interface INetworkTrafficMonitor
{
    NetworkUsage GetUsageByPid(int pid);
    IReadOnlyList<NetworkHost> GetHostsByPid(int pid);

    // THÊM METHOD MỚI - chỉ để lấy danh sách PID đang có traffic
    IEnumerable<int> GetActivePids();
}