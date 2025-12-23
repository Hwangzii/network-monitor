// file: Services/NetworkMonitor/EtwNetworkTrafficMonitor.cs
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

using NetworkMonitor.Api.Services.NetworkMonitor;
using NetworkMonitor.Api.Services.NetworkMonitor.Models;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public class EtwNetworkTrafficMonitor : INetworkTrafficMonitor
{
    private readonly ConcurrentDictionary<int, PidTrafficStats> _stats = new();


    private void OnTcpIpSend(TcpIpSendTraceData data)
        => UpdateStats(data.ProcessID, data.size, false, data.daddr.ToString());

    private void OnTcpIpRecv(TcpIpTraceData data)
        => UpdateStats(data.ProcessID, data.size, true, data.saddr.ToString());

    private void OnUdpIpSend(UdpIpTraceData data)
        => UpdateStats(data.ProcessID, data.size, false, data.daddr.ToString());

    private void OnUdpIpRecv(UdpIpTraceData data)
        => UpdateStats(data.ProcessID, data.size, true, data.saddr.ToString());

    private void UpdateStats(int pid, int size, bool isReceive, string? remoteIp)
    {
        var stat = _stats.GetOrAdd(pid, _ => new PidTrafficStats());

        Interlocked.Add(ref stat.CurrentWindowBytes[isReceive ? 0 : 1], size);

        if (!string.IsNullOrEmpty(remoteIp))
        {
            // Kiểm tra xem IP này đã có trong danh sách Host hiện tại chưa
            if (stat.CurrentHosts.Add(remoteIp))
            {
                // Nếu là IP mới phát hiện cho Process này, hãy đẩy vào Log Analysis
                string processName = GetProcessName(pid);
                
            }
        }

        if (DateTime.UtcNow.Second != stat.LastSecond)
        {
            stat.DownloadBps = stat.CurrentWindowBytes[0];
            stat.UploadBps = stat.CurrentWindowBytes[1];
            stat.Hosts = stat.CurrentHosts
                .Select(ip => new NetworkHost { RemoteIp = ip })
                .ToList();

            stat.CurrentWindowBytes[0] = stat.CurrentWindowBytes[1] = 0;
            stat.CurrentHosts.Clear();
            stat.LastSecond = DateTime.UtcNow.Second;
        }
    }

    // Hàm phụ trợ để lấy tên ứng dụng từ PID
    private string GetProcessName(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            return proc.ProcessName;
        }
        catch { return "Unknown System Process"; }
    }

    public NetworkUsage GetUsageByPid(int pid)
        => _stats.TryGetValue(pid, out var stat)
            ? new NetworkUsage { DownloadBytesPerSecond = stat.DownloadBps, UploadBytesPerSecond = stat.UploadBps }
            : new NetworkUsage();

    public IReadOnlyList<NetworkHost> GetHostsByPid(int pid)
        => _stats.TryGetValue(pid, out var stat)
            ? stat.Hosts
            : Array.Empty<NetworkHost>();


    private class PidTrafficStats
    {
        public long DownloadBps;
        public long UploadBps;
        public long[] CurrentWindowBytes = new long[2];
        public int LastSecond = DateTime.UtcNow.Second;
        public HashSet<string> CurrentHosts = new();
        public List<NetworkHost> Hosts = new();
    }

    public IEnumerable<int> GetActivePids()
{
    return _stats.Keys;
}
}
