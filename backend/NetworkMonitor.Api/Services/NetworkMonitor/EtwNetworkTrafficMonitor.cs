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
public class EtwNetworkTrafficMonitor : INetworkTrafficMonitor, IDisposable
{
    private readonly TraceEventSession _session;
    private readonly ConcurrentDictionary<int, PidTrafficStats> _stats = new();

    public EtwNetworkTrafficMonitor()
    {
        _session = new TraceEventSession("NetworkMonitorEtwSession");
        _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

        _session.Source.Kernel.TcpIpSend += OnTcpIpSend;
        _session.Source.Kernel.TcpIpRecv += OnTcpIpRecv;
        _session.Source.Kernel.UdpIpSend += OnUdpIpSend;
        _session.Source.Kernel.UdpIpRecv += OnUdpIpRecv;

        ThreadPool.QueueUserWorkItem(_ => _session.Source.Process());
    }

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
            stat.CurrentHosts.Add(remoteIp);

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

    public NetworkUsage GetUsageByPid(int pid)
        => _stats.TryGetValue(pid, out var stat)
            ? new NetworkUsage { DownloadBytesPerSecond = stat.DownloadBps, UploadBytesPerSecond = stat.UploadBps }
            : new NetworkUsage();

    public IReadOnlyList<NetworkHost> GetHostsByPid(int pid)
        => _stats.TryGetValue(pid, out var stat)
            ? stat.Hosts
            : Array.Empty<NetworkHost>();

    public void Dispose() => _session.Dispose();

    private class PidTrafficStats
    {
        public long DownloadBps;
        public long UploadBps;
        public long[] CurrentWindowBytes = new long[2];
        public int LastSecond = DateTime.UtcNow.Second;
        public HashSet<string> CurrentHosts = new();
        public List<NetworkHost> Hosts = new();
    }
}
