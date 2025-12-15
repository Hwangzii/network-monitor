// file: NetworkMonitor.Api/Services/NetworkMonitor/INetworkTrafficMonitor.cs
using NetworkMonitor.Api.Services.NetworkMonitor.Models;

namespace NetworkMonitor.Api.Services.NetworkMonitor;

public interface INetworkTrafficMonitor
{
    NetworkUsage GetUsageByPid(int pid);
    IReadOnlyList<NetworkHost> GetHostsByPid(int pid);
}