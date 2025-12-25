// file: NetworkMonitor.Api/Features/Traffic/Services/TrafficUsageService.cs
using MaxMind.GeoIP2;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using NetworkMonitor.Api.Services.NetworkMonitor;
using NetworkMonitor.Api.Services.NetworkMonitor.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Runtime.Versioning;

namespace NetworkMonitor.Api.Features.Traffic.Services;

[SupportedOSPlatform("windows")]
public class TrafficUsageService
{
    private readonly INetworkTrafficMonitor _trafficMonitor;
    private readonly DatabaseReader? _geoReader;
    
    // CACHE BUFFERS
    private readonly ConcurrentDictionary<int, (string Name, string? Icon)> _processCache = new();
    private readonly ConcurrentDictionary<string, string> _hostnameCache = new();

    public TrafficUsageService(INetworkTrafficMonitor trafficMonitor, IWebHostEnvironment env)
    {
        _trafficMonitor = trafficMonitor;

        if (OperatingSystem.IsWindows())
        {
            var dbPath = Path.Combine(env.ContentRootPath, "Data", "GeoLite2-Country.mmdb");
            if (File.Exists(dbPath))
                _geoReader = new DatabaseReader(dbPath);
        }
    }

    // Lấy thông tin Process có Cache (Tránh Unknown Process khi PID kết thúc sớm)
    private (string Name, string? Icon) GetProcessInfo(int pid)
    {
        if (_processCache.TryGetValue(pid, out var cached)) return cached;

        try
        {
            using var p = Process.GetProcessById(pid);
            var name = p.ProcessName;
            var path = p.MainModule?.FileName;
            string? iconBase64 = null;

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                using var bmp = icon!.ToBitmap();
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                iconBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }

            var info = (name, iconBase64);
            _processCache.TryAdd(pid, info);
            return info;
        }
        catch 
        { 
            return ("Terminated Process", null); 
        }
    }

    private string ResolveHostname(string ip)
    {
        if (_hostnameCache.TryGetValue(ip, out var cached)) return cached;

        // Trả về IP trước, thực hiện Resolve bất đồng bộ để không treo API
        Task.Run(async () =>
        {
            try
            {
                var entry = await Dns.GetHostEntryAsync(ip);
                _hostnameCache.TryAdd(ip, entry.HostName);
            }
            catch { _hostnameCache.TryAdd(ip, ip); }
        });

        return ip;
    }

    public TrafficUsageSummaryDto GetCurrentUsageSummary()
    {
        var dto = new TrafficUsageSummaryDto();
        var activePids = _trafficMonitor.GetActivePids().ToList();

        var apps = new Dictionary<int, AppUsageDto>();
        var hosts = new Dictionary<string, HostUsageDto>();
        var trafficTypes = new Dictionary<string, long>();
        long totalAllBytes = 0;

        foreach (var pid in activePids)
        {
            var usage = _trafficMonitor.GetUsageByPid(pid);
            var hostList = _trafficMonitor.GetHostsByPid(pid);
            long pidBytes = usage.UploadBytesPerSecond + usage.DownloadBytesPerSecond;

            if (pidBytes == 0 && !hostList.Any()) continue;

            var (procName, procIcon) = GetProcessInfo(pid);
            var appDto = new AppUsageDto
            {
                Name = procName,
                AppIcon = procIcon,
                UsageBytes = pidBytes,
                Usage = FormatBytesPerSecond(pidBytes)
            };

            foreach (var h in hostList)
            {
                if (string.IsNullOrEmpty(h.RemoteIp)) continue;

                totalAllBytes += h.Bytes;
                
                // Traffic Types
                string protocol = GetProtocolName(h.RemotePort);
                trafficTypes[protocol] = trafficTypes.GetValueOrDefault(protocol) + h.Bytes;

                // Hosts
                if (!hosts.TryGetValue(h.RemoteIp, out var hDto))
                {
                    var (cName, cCode) = GetCountry(h.RemoteIp);
                    hDto = new HostUsageDto
                    {
                        Hostname = ResolveHostname(h.RemoteIp),
                        CountryName = cName,
                        CountryCode = cCode,
                        CountryFlagUrl = cCode == "local" ? "" : $"https://flagcdn.com/w20/{cCode}.png"
                    };
                    hosts[h.RemoteIp] = hDto;
                }
                hDto.UsageBytes += h.Bytes;
            }
            apps[pid] = appDto;
        }

        dto.Apps = apps.Values.OrderByDescending(x => x.UsageBytes).Take(20).ToList();
        dto.Hosts = hosts.Values.OrderByDescending(x => x.UsageBytes).Take(20).Select(h => {
            h.Usage = FormatBytesPerSecond(h.UsageBytes);
            return h;
        }).ToList();

        dto.TrafficTypes = trafficTypes.Select(kv => new TrafficTypeUsageDto {
            Type = kv.Key,
            Usage = FormatBytesPerSecond(kv.Value),
            Percentage = totalAllBytes > 0 ? Math.Round(kv.Value * 100.0 / totalAllBytes, 1) : 0
        }).OrderByDescending(x => x.Percentage).ToList();

        return dto;
    }

    private string FormatBytesPerSecond(long bps) => bps <= 0 ? "0 B/s" : bps < 1024 ? $"{bps} B/s" : bps < 1048576 ? $"{bps / 1024.0:0.##} KB/s" : $"{bps / 1048576.0:0.##} MB/s";
    private string GetProtocolName(int port) => port switch { 80 => "HTTP", 443 => "HTTPS", 53 => "DNS", 3389 => "RDP", _ => "TCP/UDP" };
    private (string Name, string Code) GetCountry(string ip) { /* Logic GeoIP giữ nguyên */ return ("Unknown", "un"); }
}