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
    private int _eventCount = 0; // ← DEBUG: Đếm số events

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
                    using (_session = new TraceEventSession(sessionName, null))
                    {
                        Console.WriteLine("📡 ETW session created, enabling kernel provider...");

                        // Enable Network TCP/IP events
                        _session.EnableKernelProvider(
                            KernelTraceEventParser.Keywords.NetworkTCPIP,
                            KernelTraceEventParser.Keywords.None
                        );

                        Console.WriteLine("✅ Kernel provider enabled");

                        // Đăng ký các event handlers
                        var parser = _session.Source.Kernel;
                        
                        parser.TcpIpSend += OnTcpIpSend;
                        parser.TcpIpRecv += OnTcpIpRecv;
                        parser.UdpIpSend += OnUdpIpSend;
                        parser.UdpIpRecv += OnUdpIpRecv;

                        Console.WriteLine("🎯 Event handlers registered, starting event processing...");

                        // Xử lý events trong vòng lặp blocking
                        _session.Source.Process();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("⚠️ ETW Monitor: Cần chạy với quyền Administrator để bắt network traffic");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ETW Monitor Error: {ex.Message}");
                    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                }
            }, _cancellationTokenSource.Token);

            Console.WriteLine("✅ ETW Network Monitor started successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to start ETW Monitor: {ex.Message}");
        }
    }

    private void OnTcpIpSend(TcpIpSendTraceData data)
    {
        Interlocked.Increment(ref _eventCount);
        if (_eventCount % 100 == 0) // Log mỗi 100 events
            Console.WriteLine($"📊 ETW: Received {_eventCount} events, _stats.Count = {_stats.Count}");
        
        UpdateStats(data.ProcessID, data.size, false, data.daddr.ToString());
    }

    private void OnTcpIpRecv(TcpIpTraceData data)
    {
        Interlocked.Increment(ref _eventCount);
        if (_eventCount % 100 == 0)
            Console.WriteLine($"📊 ETW: Received {_eventCount} events, _stats.Count = {_stats.Count}");
        
        UpdateStats(data.ProcessID, data.size, true, data.saddr.ToString());
    }

    private void OnUdpIpSend(UdpIpTraceData data)
    {
        Interlocked.Increment(ref _eventCount);
        UpdateStats(data.ProcessID, data.size, false, data.daddr.ToString());
    }

    private void OnUdpIpRecv(UdpIpTraceData data)
    {
        Interlocked.Increment(ref _eventCount);
        UpdateStats(data.ProcessID, data.size, true, data.saddr.ToString());
    }

    private void UpdateStats(int pid, int size, bool isReceive, string? remoteIp)
    {
        var stat = _stats.GetOrAdd(pid, _ => new PidTrafficStats());

        Interlocked.Add(ref stat.CurrentWindowBytes[isReceive ? 0 : 1], size);

        if (!string.IsNullOrEmpty(remoteIp))
        {
            stat.CurrentHosts.Add(remoteIp);
        }

        // Reset mỗi giây để tính tốc độ bytes/s
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

    public IEnumerable<int> GetActivePids()
    {
        var pids = _stats.Keys.ToList();
        Console.WriteLine($"🔍 GetActivePids() called: Found {pids.Count} PIDs in _stats");
        return pids;
    }

    private class PidTrafficStats
    {
        public long DownloadBps;
        public long UploadBps;
        public long[] CurrentWindowBytes = new long[2];
        public int LastSecond = DateTime.UtcNow.Second;
        public HashSet<string> CurrentHosts = new();
        public List<NetworkHost> Hosts = new();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Console.WriteLine($"🛑 Disposing ETW Monitor. Total events received: {_eventCount}");

        try
        {
            _cancellationTokenSource?.Cancel();
            _session?.Dispose();
            _processingTask?.Wait(TimeSpan.FromSeconds(2));
            _cancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing ETW Monitor: {ex.Message}");
        }
    }
}