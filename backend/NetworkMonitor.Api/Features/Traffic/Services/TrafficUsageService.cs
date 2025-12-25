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
        
        // Dictionary để gộp dữ liệu quốc gia
        var countryAgg = new Dictionary<string, (string Name, long Bytes)>();

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

                var (cName, cCode) = GetCountry(h.RemoteIp);
                
                // XỬ LÝ THEO YÊU CẦU: Nếu là local network thì chuyển thành VN
                if (cCode == "local") 
                {
                    cCode = "vn";
                    cName = "Vietnam";
                }

                // Gộp dữ liệu quốc gia cho danh sách Countries (bỏ qua "un" - Unknown)
                if (cCode != "un")
                {
                    if (!countryAgg.ContainsKey(cCode))
                        countryAgg[cCode] = (cName, h.Bytes);
                    else
                        countryAgg[cCode] = (cName, countryAgg[cCode].Bytes + h.Bytes);
                }
                
                // Traffic Types
                string protocol = GetProtocolName(h.RemotePort);
                trafficTypes[protocol] = trafficTypes.GetValueOrDefault(protocol) + h.Bytes;

                // Hosts
                if (!hosts.TryGetValue(h.RemoteIp, out var hDto))
                {
                    hDto = new HostUsageDto
                    {
                        Hostname = ResolveHostname(h.RemoteIp),
                        CountryName = cName,
                        CountryCode = cCode,
                        CountryFlagUrl = $"https://flagcdn.com/w20/{cCode}.png"
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

        // Đổ dữ liệu gộp vào danh sách Countries của DTO
        dto.Countries = countryAgg.Select(kv => new CountryUsageDto
        {
            CountryCode = kv.Key,
            CountryName = kv.Value.Name,
            UsageBytes = kv.Value.Bytes,
            Usage = FormatBytesPerSecond(kv.Value.Bytes),
            FlagUrl = $"https://flagcdn.com/w40/{kv.Key}.png"
        }).OrderByDescending(c => c.UsageBytes).ToList();

        return dto;
    }

    private string FormatBytesPerSecond(long bps) => bps <= 0 ? "0 B/s" : bps < 1024 ? $"{bps} B/s" : bps < 1048576 ? $"{bps / 1024.0:0.##} KB/s" : $"{bps / 1048576.0:0.##} MB/s";
    private string GetProtocolName(int port) => port switch { 80 => "HTTP", 443 => "HTTPS", 53 => "DNS", 3389 => "RDP", _ => "TCP/UDP" };
    private (string Name, string Code) GetCountry(string ip)
    {
        if (string.IsNullOrEmpty(ip) || ip == "127.0.0.1" || ip.StartsWith("192.168.")) 
            return ("Local Network", "local");

        if (_geoReader != null)
        {
            try
            {
                // Tra cứu quốc gia từ file GeoLite2-Country.mmdb
                var response = _geoReader.Country(ip);
                return (response.Country.Name ?? "Unknown", response.Country.IsoCode?.ToLower() ?? "un");
            }
            catch { /* IP không có trong DB */ }
        }
        return ("Unknown", "un");
    }
}