// file: NetworkMonitor.Api/Features/Traffic/Services/TrafficUsageService.cs
using MaxMind.GeoIP2;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using NetworkMonitor.Api.Services.NetworkMonitor;
using NetworkMonitor.Api.Services.NetworkMonitor.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace NetworkMonitor.Api.Features.Traffic.Services;

public class TrafficUsageService
{
    private readonly INetworkTrafficMonitor _trafficMonitor;
    private readonly DatabaseReader? _geoReader;
    private readonly ConcurrentDictionary<int, string> _pidToExePathCache = new();
    private readonly ConcurrentDictionary<int, string> _pidToProcessNameCache = new();

    public TrafficUsageService(INetworkTrafficMonitor trafficMonitor, IWebHostEnvironment env)
    {
        _trafficMonitor = trafficMonitor;

        if (OperatingSystem.IsWindows())
        {
            var dbPath = Path.Combine(env.ContentRootPath, "Data", "GeoLite2-Country.mmdb");
            if (File.Exists(dbPath))
            {
                _geoReader = new DatabaseReader(dbPath);
            }
        }
    }

    private string FormatBytesPerSecond(long bps)
    {
        if (bps == 0) return "0 B/s";

        double value = bps;
        string[] suffixes = { "B/s", "KB/s", "MB/s", "GB/s" };
        int i = 0;
        while (value >= 1024 && i < suffixes.Length - 1)
        {
            value /= 1024;
            i++;
        }
        return $"{value:0.##} {suffixes[i]}";
    }

#pragma warning disable CA1416 // Chỉ chạy trên Windows - đã kiểm tra ở trên
    private string? GetIconBase64FromPid(int pid)
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var process = Process.GetProcessById(pid);
            string? exePath = process.MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return null;

            using var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon == null) return null;

            using var bmp = icon.ToBitmap();
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
        }
    }
#pragma warning restore CA1416

    private string GetProcessName(int pid)
    {
        if (_pidToProcessNameCache.TryGetValue(pid, out var cachedName))
            return cachedName;

        try
        {
            using var process = Process.GetProcessById(pid);
            var name = process.ProcessName;
            _pidToProcessNameCache[pid] = name;
            return name;
        }
        catch
        {
            var unknown = "Unknown Process";
            _pidToProcessNameCache[pid] = unknown;
            return unknown;
        }
    }

    private string? GetExePath(int pid)
    {
        if (_pidToExePathCache.TryGetValue(pid, out var cachedPath))
            return cachedPath;

        try
        {
            using var process = Process.GetProcessById(pid);
            var path = process.MainModule?.FileName;
            if (!string.IsNullOrEmpty(path))
                _pidToExePathCache[pid] = path;
            return path;
        }
        catch
        {
            return null;
        }
    }

    private (string CountryName, string CountryCode) GetCountry(string ip)
    {
        if (_geoReader == null || string.IsNullOrWhiteSpace(ip))
            return ("Unknown", "xx");

        // Local/private IPs
        if (ip.StartsWith("127.") || ip.StartsWith("192.168.") ||
            ip.StartsWith("10.") || (ip.StartsWith("172.") && ip.Length >= 9 && ip[4..9].CompareTo("16.") >= 0 && ip[4..9].CompareTo("31.") <= 0) ||
            ip == "::1")
            return ("Local Network", "local");

        try
        {
            var response = _geoReader.Country(ip);
            return (response.Country.Name ?? "Unknown", response.Country.IsoCode?.ToLower() ?? "xx");
        }
        catch
        {
            return ("Unknown", "xx");
        }
    }

    public TrafficUsageSummaryDto GetCurrentUsageSummary()
    {
        var dto = new TrafficUsageSummaryDto();

        // DÙNG METHOD MỚI - an toàn, không truy cập private field
        var activePids = _trafficMonitor.GetActivePids();

        var appDict = new Dictionary<int, (long uploadBps, long downloadBps, string name, string? exePath)>();
        var hostSet = new HashSet<string>();
        var countryDict = new Dictionary<string, long>(); // "Name|Code" -> total bps

        foreach (var pid in activePids)
        {
            var usage = _trafficMonitor.GetUsageByPid(pid);
            var hosts = _trafficMonitor.GetHostsByPid(pid);

            long totalBps = usage.UploadBytesPerSecond + usage.DownloadBytesPerSecond;
            if (totalBps == 0 && !hosts.Any()) continue;

            string processName = GetProcessName(pid);
            string? exePath = GetExePath(pid);

            appDict[pid] = (usage.UploadBytesPerSecond, usage.DownloadBytesPerSecond, processName, exePath);

            // Thu thập hosts + country (gán bps của process cho tất cả host của nó)
            foreach (var host in hosts)
            {
                if (!string.IsNullOrEmpty(host.RemoteIp))
                {
                    hostSet.Add(host.RemoteIp);

                    var (countryName, countryCode) = GetCountry(host.RemoteIp);
                    string key = $"{countryName}|{countryCode}";
                    countryDict[key] = countryDict.GetValueOrDefault(key) + totalBps;
                }
            }
        }

        long totalBpsAll = appDict.Values.Sum(x => x.uploadBps + x.downloadBps);

        // === Apps ===
        foreach (var kv in appDict.OrderByDescending(x => x.Value.uploadBps + x.Value.downloadBps).Take(15))
        {
            long totalBps = kv.Value.uploadBps + kv.Value.downloadBps;

            // Lấy country phổ biến nhất làm đại diện cho app
            var topCountry = countryDict.OrderByDescending(c => c.Value).FirstOrDefault();
            var (countryName, countryCode) = topCountry.Key?.Split('|') is string[] parts && parts.Length == 2
                ? (parts[0], parts[1])
                : ("Unknown", "xx");

            dto.Apps.Add(new AppUsageDto
            {
                Name = kv.Value.name,
                Usage = FormatBytesPerSecond(totalBps),
                UsageBytes = totalBps, // current rate (bytes/s)
                AppIcon = GetIconBase64FromPid(kv.Key),
                CountryName = countryName,
                CountryCode = countryCode,
                CountryFlagUrl = $"https://flagcdn.com/w20/{countryCode}.png"
            });
        }

        // === Hosts ===
        foreach (var ip in hostSet.OrderByDescending(ip =>
        {
            var (cn, cc) = GetCountry(ip);
            return countryDict.GetValueOrDefault($"{cn}|{cc}");
        }).Take(15))
        {
            var (countryName, countryCode) = GetCountry(ip);

            dto.Hosts.Add(new HostUsageDto
            {
                Hostname = ip,
                Usage = "Active", // chưa có per-host bytes
                UsageBytes = 0,
                AppOwnerIcon = null, // tạm thời không biết app nào
                CountryName = countryName,
                CountryCode = countryCode,
                CountryFlagUrl = $"https://flagcdn.com/w20/{countryCode}.png"
            });
        }

        // === Traffic Types ===
        if (totalBpsAll > 0)
        {
            dto.TrafficTypes.Add(new TrafficTypeUsageDto
            {
                Type = "TCP/UDP Traffic",
                Usage = FormatBytesPerSecond(totalBpsAll),
                Percentage = 100.0
            });
        }

        // === Countries ===
        foreach (var kv in countryDict.OrderByDescending(x => x.Value).Take(10))
        {
            var parts = kv.Key.Split('|');
            if (parts.Length < 2) continue;

            dto.Countries.Add(new CountryUsageDto
            {
                CountryName = parts[0],
                CountryCode = parts[1],
                Usage = FormatBytesPerSecond(kv.Value),
                FlagUrl = $"https://flagcdn.com/w40/{parts[1]}.png"
            });
        }

        return dto;
    }
}