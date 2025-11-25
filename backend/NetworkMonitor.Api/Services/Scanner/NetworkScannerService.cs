// File: NetworkMonitor.Api/Services/Scanner/NetworkScannerService.cs
// ĐÃ CHUYỂN HOÀN TOÀN SANG TIẾNG ANH – CHUẨN GLASSWIRE

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text.RegularExpressions;
using LiteDB;
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Models;

namespace NetworkMonitor.Api.Services.Scanner;

public class NetworkScannerService : INetworkScannerService
{
    private readonly ILogger<NetworkScannerService> _logger;
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<KnownDevice> _devices;

    public NetworkScannerService(ILogger<NetworkScannerService> logger)
    {
        _logger = logger;

        // ĐƯỜNG DẪN MỚI: backend/Data/devices.db
        var projectRoot = Directory.GetCurrentDirectory();
        var dbPath = Path.Combine(projectRoot, "Data", "devices.db");

        // Tự động tạo thư mục Data nếu chưa có
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _db = new LiteDatabase(dbPath);
        _devices = _db.GetCollection<KnownDevice>("devices");
        _devices.EnsureIndex(x => x.Mac);
    }

    public async Task<IEnumerable<NetworkDeviceDto>> ScanNetworkAsync()
    {
        if (!OperatingSystem.IsWindows())
            return Enumerable.Empty<NetworkDeviceDto>();

        var gatewayInfo = GetDefaultGateway();
        if (gatewayInfo == null)
        {
            _logger.LogWarning("No default gateway found.");
            return Enumerable.Empty<NetworkDeviceDto>();
        }

        var (gatewayIp, _) = gatewayInfo.Value;
        var localIp = GetLocalIPAddress();
        var subnet = GetSubnetFromGateway(gatewayIp);

        _logger.LogInformation("Scanning network: {Subnet} | Gateway: {Gateway}", subnet, gatewayIp);

        var activeIps = await PingSweepAsync(subnet);
        var arpTable = GetArpTable();

        var now = DateTime.UtcNow;
        var onlineMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<NetworkDeviceDto>();

        foreach (var ip in activeIps)
        {
            var ipStr = ip.ToString();
            var mac = arpTable.GetValueOrDefault(ipStr, "N/A");
            if (mac != "N/A") onlineMacs.Add(mac);

            var device = await UpdateOrCreateDevice(ipStr, mac, localIp, gatewayIp, now);
            result.Add(device);
        }

        // Add offline devices (previously seen)
        var allKnown = _devices.FindAll();
        foreach (var known in allKnown)
        {
            if (!onlineMacs.Contains(known.Mac) && known.LastSeen < now.AddMinutes(-5))
            {
                if (result.All(x => !string.Equals(x.Mac, known.Mac, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(ToDto(known, false, now));
                }
            }
        }

        return result.OrderBy(x => x.Ip, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<NetworkDeviceDto> UpdateOrCreateDevice(string ip, string mac, IPAddress? localIp, IPAddress gatewayIp, DateTime now)
    {
        KnownDevice? known = mac != "N/A" ? _devices.FindOne(x => x.Mac == mac) : null;

        if (known == null && mac != "N/A")
        {
            known = new KnownDevice
            {
                Mac = mac,
                FirstSeen = now,
                LastSeen = now,
                Vendor = "Unknown",
                DeviceType = "Generic",
                KnownIps = new List<string>(),
                KnownHostnames = new List<string>()
            };
        }

        if (known != null)
        {
            known.LastSeen = now;
            if (!known.KnownIps.Contains(ip)) known.KnownIps.Add(ip);

            try
            {
                var entry = await Dns.GetHostEntryAsync(ip);
                var name = entry.HostName.Split('.')[0];
                if (!known.KnownHostnames.Contains(name))
                    known.KnownHostnames.Add(name);
            }
            catch { }

            var (vendor, type) = GetVendorAndTypeFromMac(mac);
            known.Vendor = vendor;
            known.DeviceType = type;

            _devices.Upsert(known);
        }

        var dto = ToDto(known ?? new KnownDevice { Mac = mac, FirstSeen = now, LastSeen = now, Vendor = "Unknown", DeviceType = "Generic" }, true, now);

        dto.Ip = ip;
        dto.IsOnline = true;

        if (ip == gatewayIp.ToString())
        {
            dto.Type = "Router";
            dto.Name = known?.KnownHostnames.LastOrDefault() ?? "Router";
            dto.Description = "Default Gateway";
        }
        else if (localIp != null && ip == localIp.ToString())
        {
            dto.Type = "This PC";
            dto.Name = Environment.MachineName;
            dto.Description = $"{Environment.MachineName} (This Device)";
        }
        else
        {
            dto.Type = known?.DeviceType ?? "Generic";
            dto.Name = known?.KnownHostnames.LastOrDefault() ?? "Unknown";
            dto.Description = known?.Vendor ?? "Unknown";
        }

        return dto;
    }

    private NetworkDeviceDto ToDto(KnownDevice device, bool isOnline, DateTime now)
    {
        return new NetworkDeviceDto
        {
            Mac = device.Mac,
            FirstSeen = device.FirstSeen,
            LastSeen = device.LastSeen,
            IsOnline = isOnline,
            LastSeenText = FormatLastSeen(device.LastSeen, now),
            Name = device.KnownHostnames.LastOrDefault() ?? "Unknown",
            Description = device.Vendor,
            Type = device.DeviceType,
            Ip = "N/A",
            Location = "",
            System = ""
        };
    }

    private string FormatLastSeen(DateTime lastSeen, DateTime now)
    {
        var diff = now - lastSeen;

        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minute{(diff.TotalMinutes >= 2 ? "s" : "")} ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour{(diff.TotalHours >= 2 ? "s" : "")} ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} day{(diff.TotalDays >= 2 ? "s" : "")} ago";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} week{(diff.TotalDays / 7 >= 2 ? "s" : "")} ago";
        if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} month{(diff.TotalDays / 30 >= 2 ? "s" : "")} ago";

        return "A long time ago";
    }

    #region Helper Methods

    private (IPAddress Gateway, NetworkInterface Interface)? GetDefaultGateway()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet && ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;

            var gateway = ni.GetIPProperties().GatewayAddresses
                .FirstOrDefault(g => g?.Address?.AddressFamily == AddressFamily.InterNetwork)?.Address;

            if (gateway != null && !IPAddress.IsLoopback(gateway))
                return (gateway, ni);
        }
        return null;
    }

    private IPAddress? GetLocalIPAddress()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            var addr = ni.GetIPProperties().UnicastAddresses
                .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address));
            if (addr?.Address != null) return addr.Address;
        }
        return null;
    }

    private string GetSubnetFromGateway(IPAddress ip)
        => $"{ip.GetAddressBytes()[0]}.{ip.GetAddressBytes()[1]}.{ip.GetAddressBytes()[2]}.0/24";

    private async Task<List<IPAddress>> PingSweepAsync(string subnetCidr)
    {
        var parts = subnetCidr.Split('/')[0].Split('.');
        var network = $"{parts[0]}.{parts[1]}.{parts[2]}.";
        var activeIps = new List<IPAddress>();
        var tasks = new List<Task>();

        for (int i = 1; i <= 254; i++)
        {
            var ip = IPAddress.Parse(network + i);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ip, 800);
                    if (reply.Status == IPStatus.Success)
                        lock (activeIps) activeIps.Add(ip);
                }
                catch { }
            }));
        }

        await Task.WhenAll(tasks);
        return activeIps;
    }

    private Dictionary<string, string> GetArpTable()
    {
        var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("arp", "-a")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            var regex = new Regex(@"(?<ip>\d{1,3}(\.\d{1,3}){3})\s+(?<mac>[0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2})");
            foreach (Match m in regex.Matches(output))
            {
                var ip = m.Groups["ip"].Value.Trim();
                var mac = m.Groups["mac"].Value.Trim().Replace("-", ":");
                table[ip] = mac.ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read ARP table (normal on first scan)");
        }
        return table;
    }

    private (string Vendor, string Type) GetVendorAndTypeFromMac(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac) || mac.Length < 8) return ("Unknown", "Generic");
        var oui = mac[..8].Replace(":", "").ToUpperInvariant();

        var map = new Dictionary<string, (string, string)>
        {
            {"B0B867", ("TP-Link", "Router")}, {"C8D3A3", ("TP-Link", "Router")}, {"F81A67", ("TP-Link", "Router")},
            {"6CE8B6", ("Huawei", "Router")}, {"C40D96", ("Huawei", "Wi-Fi")},
            {"D4F4BE", ("Apple", "iPhone/iPad")}, {"F4F5D8", ("Apple", "iPhone/iPad")}, {"04E536", ("Apple", "iPhone/iPad")},
            {"E029E9", ("Lenovo", "Laptop")}, {"F49634", ("Intel", "Laptop/PC")}, {"E0B9BA", ("Samsung", "Smart TV")}
        };

        return map.TryGetValue(oui, out var v) ? v : ("Unknown", "Generic");
    }

    #endregion
}

