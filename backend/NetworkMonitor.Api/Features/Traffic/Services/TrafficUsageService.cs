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
using System.Linq;

namespace NetworkMonitor.Api.Features.Traffic.Services;

public class TrafficUsageService
{
    private readonly INetworkTrafficMonitor _trafficMonitor;
    private readonly DatabaseReader? _geoReader;
    private readonly ConcurrentDictionary<int, string> _pidCacheName = new();
    private readonly ConcurrentDictionary<int, string?> _pidCacheExe = new();

    public TrafficUsageService(INetworkTrafficMonitor trafficMonitor, IWebHostEnvironment env)
    {
        _trafficMonitor = trafficMonitor;

        if (OperatingSystem.IsWindows())
        {
            var dbPath = Path.Combine(env.ContentRootPath, "Data", "GeoLite2-Country.mmdb");
            if (File.Exists(dbPath))
            {
                _geoReader = new DatabaseReader(dbPath);
                Console.WriteLine("✅ GeoLite2-Country.mmdb loaded successfully!");
            }
            else
            {
                Console.WriteLine("⚠️ GeoLite2-Country.mmdb not found → unknown countries will use 'un' flag");
            }
        }
    }

    private string FormatBytesPerSecond(long bps)
    {
        if (bps == 0) return "0 B/s";
        if (bps < 1024) return $"{bps} B/s";
        if (bps < 1024 * 1024) return $"{bps / 1024.0:0.##} KB/s";
        if (bps < 1024L * 1024 * 1024) return $"{bps / (1024.0 * 1024):0.##} MB/s";
        return $"{bps / (1024.0 * 1024 * 1024):0.##} GB/s";
    }

#pragma warning disable CA1416
    private string? GetIconBase64FromPid(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            var path = p.MainModule?.FileName;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            using var icon = Icon.ExtractAssociatedIcon(path);
            using var bmp = icon!.ToBitmap();
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        }
        catch { return null; }
    }
#pragma warning restore CA1416

    private string GetProcessName(int pid)
        => _pidCacheName.GetOrAdd(pid, p =>
        {
            try { return Process.GetProcessById(p).ProcessName; }
            catch { return "Unknown Process"; }
        });

    private (string Name, string Code) GetCountry(string ip)
    {
        if (_geoReader == null || string.IsNullOrWhiteSpace(ip))
            return ("Unknown", "un");

        if (IsLocalIp(ip))
            return ("Local Network", "local");

        try
        {
            var response = _geoReader.Country(ip);
            var code = response.Country.IsoCode?.Trim().ToLower();
            if (string.IsNullOrEmpty(code))
                return ("Unknown", "un");

            var name = response.Country.Name ?? code.ToUpper();
            return (name, code);
        }
        catch
        {
            return ("Unknown", "un");
        }
    }

    private bool IsLocalIp(string ip)
    {
        return ip.StartsWith("10.") ||
               ip.StartsWith("192.168.") ||
               ip.StartsWith("127.") ||
               (ip.StartsWith("172.") && int.TryParse(ip.Split('.')[1], out int n) && n >= 16 && n <= 31) ||
               ip == "::1" ||
               ip.StartsWith("fe80::") ||
               ip.StartsWith("fc00::");
    }

    private string GetProtocolName(int port) => port switch
    {
        80 => "Hypertext Transfer Protocol (HTTP)",
        443 => "Hypertext Transfer Protocol over SSL/TLS (HTTPS)",
        53 => "Domain Name System (DNS)",
        8080 => "HTTP Alternate",
        25 => "Simple Mail Transfer Protocol (SMTP)",
        21 => "File Transfer Protocol (FTP)",
        22 => "Secure Shell (SSH)",
        3389 => "Remote Desktop Protocol (RDP)",
        _ => "TCP/UDP"
    };

    private string? ResolveHostname(string ip)
    {
        try { return Dns.GetHostEntry(ip).HostName; }
        catch { return null; }
    }

    public TrafficUsageSummaryDto GetCurrentUsageSummary()
    {
        var dto = new TrafficUsageSummaryDto();
        var activePids = _trafficMonitor.GetActivePids().ToList();

        var apps = new Dictionary<int, AppUsageDto>();
        var hosts = new Dictionary<string, HostUsageDto>();
        var countries = new Dictionary<string, long>(); // "Name|Code" → bytes (chỉ quốc gia thật)
        var types = new Dictionary<string, long>();

        foreach (var pid in activePids)
        {
            var usage = _trafficMonitor.GetUsageByPid(pid);
            var hostList = _trafficMonitor.GetHostsByPid(pid);

            long totalBps = usage.UploadBytesPerSecond + usage.DownloadBytesPerSecond;
            if (totalBps == 0 && !hostList.Any()) continue;

            var app = new AppUsageDto
            {
                Name = GetProcessName(pid),
                Usage = FormatBytesPerSecond(totalBps),
                UsageBytes = totalBps,
                AppIcon = GetIconBase64FromPid(pid)
                // Country mặc định đã là "un" trong DTO → không cần set lại
            };

            var countryVotes = new Dictionary<string, long>();

            foreach (var h in hostList)
            {
                if (string.IsNullOrEmpty(h.RemoteIp)) continue;

                var (countryName, countryCode) = GetCountry(h.RemoteIp);

                // Luôn cộng vào traffic types
                string protocol = GetProtocolName(h.RemotePort);
                types[protocol] = types.GetValueOrDefault(protocol) + h.Bytes;

                // Chỉ thêm vào countries nếu là quốc gia thật (không local, không un)
                if (countryCode != "local" && countryCode != "un")
                {
                    string key = $"{countryName}|{countryCode}";
                    countries[key] = countries.GetValueOrDefault(key) + h.Bytes;
                    countryVotes[countryCode] = countryVotes.GetValueOrDefault(countryCode) + h.Bytes;
                }

                // Aggregate host
                if (!hosts.TryGetValue(h.RemoteIp, out var hostDto))
                {
                    hostDto = new HostUsageDto
                    {
                        Hostname = ResolveHostname(h.RemoteIp) ?? h.RemoteIp
                    };
                    hosts[h.RemoteIp] = hostDto;
                }
                hostDto.UsageBytes += h.Bytes;
                hostDto.Usage = FormatBytesPerSecond(hostDto.UsageBytes);
                hostDto.CountryName = countryName;
                hostDto.CountryCode = countryCode;
                hostDto.CountryFlagUrl = countryCode == "local" ? "" : $"https://flagcdn.com/w20/{countryCode}.png";
            }

            // Gán country cho app nếu có quốc gia thật
            if (countryVotes.Any())
            {
                var top = countryVotes.OrderByDescending(x => x.Value).First();
                app.CountryName = top.Key.ToUpper();
                app.CountryCode = top.Key;
                app.CountryFlagUrl = $"https://flagcdn.com/w20/{top.Key}.png";
            }
            // Nếu không có quốc gia thật → giữ mặc định "un" từ DTO

            apps[pid] = app;
        }

        // Fill DTO
        dto.Apps.AddRange(apps.Values.OrderByDescending(a => a.UsageBytes).Take(20));
        dto.Hosts.AddRange(hosts.Values.OrderByDescending(h => h.UsageBytes).Take(20));

        long totalAll = apps.Values.Sum(a => a.UsageBytes);

        // Traffic Types – luôn có ít nhất 1 entry
        if (!types.Any() && totalAll > 0)
            types["TCP/UDP"] = totalAll;

        dto.TrafficTypes.AddRange(types.OrderByDescending(x => x.Value).Select(x => new TrafficTypeUsageDto
        {
            Type = x.Key,
            Usage = FormatBytesPerSecond(x.Value),
            Percentage = totalAll > 0 ? Math.Round(x.Value * 100.0 / totalAll, 1) : 0
        }));

        // Countries – chỉ thêm quốc gia thật
        dto.Countries.AddRange(countries.OrderByDescending(x => x.Value).Take(10).Select(x =>
        {
            var parts = x.Key.Split('|');
            return new CountryUsageDto
            {
                CountryName = parts[0],
                CountryCode = parts[1],
                Usage = FormatBytesPerSecond(x.Value),
                FlagUrl = $"https://flagcdn.com/w40/{parts[1]}.png"
            };
        }));

        return dto;
    }
}