// file: Services/NetworkMonitor/EtwNetworkTrafficMonitor.cs
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using NetworkMonitor.Api.Services.NetworkMonitor.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor.Api.Services.NetworkMonitor;

[SupportedOSPlatform("windows")]
public class EtwNetworkTrafficMonitor : INetworkTrafficMonitor, IDisposable
{
    private readonly ConcurrentDictionary<int, PidTrafficStats> _stats = new();
    private TraceEventSession? _session;
    private Task? _processingTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed = false;
    private int _eventCount = 0;

    public EtwNetworkTrafficMonitor()
    {
        Console.WriteLine("🔧 EtwNetworkTrafficMonitor constructor called");
        StartMonitoring();
    }

    private void StartMonitoring()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var sessionName = "NetworkMonitorSession_" + Guid.NewGuid().ToString("N");
            Console.WriteLine($"🚀 Starting ETW session: {sessionName}");

            _processingTask = Task.Run(() =>
            {
                try
                {
                    using (_session = new TraceEventSession(sessionName))
                    {
                        _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                        var parser = _session.Source.Kernel;

                        parser.TcpIpSend += OnTcpIpSend;
                        parser.TcpIpRecv += OnTcpIpRecv;
                        parser.UdpIpSend += OnUdpIpSend;
                        parser.UdpIpRecv += OnUdpIpRecv;

                        Console.WriteLine("✅ ETW handlers registered - processing events...");
                        _session.Source.Process(); // Blocking call
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("⚠️ ETW requires Administrator rights to capture network traffic.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ETW Error: {ex.Message}\n{ex.StackTrace}");
                }
            }, _cancellationTokenSource.Token);

            Console.WriteLine("✅ ETW Network Monitor started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to start ETW: {ex.Message}");
        }
    }

    private void OnTcpIpSend(TcpIpSendTraceData data) => ProcessEvent(data.ProcessID, data.size, false, data.daddr.ToString(), data.dport);
    private void OnTcpIpRecv(TcpIpTraceData data) => ProcessEvent(data.ProcessID, data.size, true, data.saddr.ToString(), data.sport);
    private void OnUdpIpSend(UdpIpTraceData data) => ProcessEvent(data.ProcessID, data.size, false, data.daddr.ToString(), data.dport);
    private void OnUdpIpRecv(UdpIpTraceData data) => ProcessEvent(data.ProcessID, data.size, true, data.saddr.ToString(), data.sport);

    private void ProcessEvent(int pid, int size, bool isReceive, string remoteIp, int remotePort)
    {
        Interlocked.Increment(ref _eventCount);
        if (_eventCount % 200 == 0)
            Console.WriteLine($"📊 ETW Events: {_eventCount} | Active PIDs: {_stats.Count}");

        var stat = _stats.GetOrAdd(pid, _ => new PidTrafficStats());

        Interlocked.Add(ref stat.CurrentWindowBytes[isReceive ? 0 : 1], size);

        if (!string.IsNullOrEmpty(remoteIp))
        {
            var key = $"{remoteIp}:{remotePort}";
            stat.CurrentHostTraffic.AddOrUpdate(key, size, (_, old) => old + size);
        }

        if (DateTime.UtcNow.Second != stat.LastSecond)
        {
            stat.DownloadBps = stat.CurrentWindowBytes[0];
            stat.UploadBps = stat.CurrentWindowBytes[1];

            // Cập nhật cumulative và tạo danh sách Hosts
            foreach (var kv in stat.CurrentHostTraffic)
            {
                stat.CumulativeHostTraffic.AddOrUpdate(kv.Key, kv.Value, (_, old) => old + kv.Value);
            }

            stat.Hosts = stat.CumulativeHostTraffic.Select(kv =>
            {
                var parts = kv.Key.Split(':');
                return new NetworkHost
                {
                    RemoteIp = parts[0],
                    RemotePort = int.Parse(parts[1]),
                    Bytes = kv.Value
                };
            }).ToList();

            // Reset window
            stat.CurrentWindowBytes[0] = stat.CurrentWindowBytes[1] = 0;
            stat.CurrentHostTraffic.Clear();
            stat.LastSecond = DateTime.UtcNow.Second;
        }
    }

    public NetworkUsage GetUsageByPid(int pid)
        => _stats.TryGetValue(pid, out var stat)
            ? new NetworkUsage { DownloadBytesPerSecond = stat.DownloadBps, UploadBytesPerSecond = stat.UploadBps }
            : new NetworkUsage();

    public IReadOnlyList<NetworkHost> GetHostsByPid(int pid)
        => _stats.TryGetValue(pid, out var stat) ? stat.Hosts : Array.Empty<NetworkHost>();

    public IEnumerable<int> GetActivePids() => _stats.Keys;

    private class PidTrafficStats
    {
        public long DownloadBps = 0;
        public long UploadBps = 0;
        public long[] CurrentWindowBytes = new long[2];
        public int LastSecond = DateTime.UtcNow.Second;

        public ConcurrentDictionary<string, long> CurrentHostTraffic = new();     // ip:port → bytes trong giây hiện tại
        public ConcurrentDictionary<string, long> CumulativeHostTraffic = new(); // ip:port → total bytes
        public List<NetworkHost> Hosts = new();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cancellationTokenSource?.Cancel();
        _session?.Dispose();
        _processingTask?.Wait(2000);
        _cancellationTokenSource?.Dispose();
    }
}