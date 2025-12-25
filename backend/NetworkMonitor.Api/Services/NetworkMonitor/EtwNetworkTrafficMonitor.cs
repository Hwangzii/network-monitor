using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using NetworkMonitor.Api.Services.NetworkMonitor.Models;
using System.Collections.Concurrent;
using System.Runtime.Versioning;

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

    // Tên session cố định để dễ dàng kiểm soát và dọn dẹp
    private const string SessionName = "NetworkMonitor_Internal_Session";

    public EtwNetworkTrafficMonitor()
    {
        Console.WriteLine("🔧 EtwNetworkTrafficMonitor constructor called");
        StartMonitoring();
    }

    private void StartMonitoring()
    {
        try
        {
            // 1. KIỂM TRA VÀ DỌN DẸP SESSION CŨ (Nếu app trước đó crash hoặc Ctrl+C chưa dọn kịp)
            var activeSessions = TraceEventSession.GetActiveSessionNames();
            if (activeSessions.Contains(SessionName))
            {
                Console.WriteLine($"🧹 Found existing session '{SessionName}'. Stopping it...");
                using var oldSession = new TraceEventSession(SessionName);
                oldSession.Stop(true);
            }

            _cancellationTokenSource = new CancellationTokenSource();

            _processingTask = Task.Run(() =>
            {
                try
                {
                    // 2. KHỞI TẠO SESSION VỚI TÊN CỐ ĐỊNH
                    using (_session = new TraceEventSession(SessionName))
                    {
                        // Đảm bảo session dừng ngay khi đối tượng bị dispose
                        _session.StopOnDispose = true;
                        
                        _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                        var parser = _session.Source.Kernel;

                        parser.TcpIpSend += OnTcpIpSend;
                        parser.TcpIpRecv += OnTcpIpRecv;
                        parser.UdpIpSend += OnUdpIpSend;
                        parser.UdpIpRecv += OnUdpIpRecv;

                        Console.WriteLine("✅ ETW handlers registered - processing events...");
                        _session.Source.Process(); 
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ETW Processing Error: {ex.Message}");
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
        // Log mỗi 500 event để tránh tràn terminal
        if (_eventCount % 500 == 0)
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

            foreach (var kv in stat.CurrentHostTraffic)
            {
                stat.CumulativeHostTraffic.AddOrUpdate(kv.Key, kv.Value, (_, old) => old + kv.Value);
            }

            stat.Hosts = stat.CumulativeHostTraffic.Select(kv =>
            {
                var parts = kv.Key.Split(':');
                return new NetworkHost { RemoteIp = parts[0], RemotePort = int.Parse(parts[1]), Bytes = kv.Value };
            }).ToList();

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
        public ConcurrentDictionary<string, long> CurrentHostTraffic = new();
        public ConcurrentDictionary<string, long> CumulativeHostTraffic = new();
        public List<NetworkHost> Hosts = new();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Console.WriteLine("🛑 Disposing ETW Monitor...");
        _cancellationTokenSource?.Cancel();
        
        if (_session != null)
        {
            _session.Stop(); // Chủ động dừng session
            _session.Dispose();
        }

        _processingTask?.Wait(1000);
        _cancellationTokenSource?.Dispose();
    }
}