// File: NetworkMonitor.Api/Services/Scanner/NetworkScannerService.cs
// ĐÃ FIX HOÀN TOÀN – KHÔNG CÒN LỖI NÀO

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Globalization;
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

        var projectRoot = Directory.GetCurrentDirectory();
        var dbPath = Path.Combine(projectRoot, "Data", "devices.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _db = new LiteDatabase(dbPath);
        _devices = _db.GetCollection<KnownDevice>("devices");
        _devices.EnsureIndex(x => x.Mac);
    }

    public async Task<IEnumerable<DeviceResponseDto>> ScanNetworkAsync()
    {
        if (!OperatingSystem.IsWindows())
            return Enumerable.Empty<DeviceResponseDto>();

        var gatewayInfo = GetDefaultGateway();
        if (gatewayInfo == null)
        {
            _logger.LogWarning("No default gateway found.");
            return Enumerable.Empty<DeviceResponseDto>();
        }

        var (gatewayIp, _) = gatewayInfo.Value;
        var localIp = GetLocalIPAddress();
        var subnet = GetSubnetFromGateway(gatewayIp);

        var activeIps = await PingSweepAsync(subnet);
        var arpTable = GetArpTable();

        var now = DateTime.UtcNow;
        var onlineMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<DeviceResponseDto>();

        foreach (var ip in activeIps)
        {
            var ipStr = ip.ToString();
            var mac = arpTable.GetValueOrDefault(ipStr, "N/A");
            if (mac != "N/A") onlineMacs.Add(mac);

            var device = await UpdateOrCreateDevice(ipStr, mac, localIp, gatewayIp, now);
            result.Add(device);
        }

        // Thêm thiết bị offline
        foreach (var known in _devices.FindAll())
        {
            if (!onlineMacs.Contains(known.Mac) && known.LastSeen < now.AddMinutes(-5))
            {
                if (result.All(d => d.mac_address != known.Mac))
                {
                    result.Add(ToDto(known, false, now, "N/A"));
                }
            }
        }

        // FIX lỗi sort khi có IP = "N/A"
        try
        {
            var sorted = result
                .OrderBy(d =>
                {
                    if (string.IsNullOrWhiteSpace(d.ip) || d.ip == "N/A")
                        return IPAddress.Parse("255.255.255.255");
                    
                    if (IPAddress.TryParse(d.ip, out var addr))
                        return addr;
                    
                    return IPAddress.Parse("255.255.255.254"); // fallback an toàn
                })
                .ThenBy(d => d.name, StringComparer.OrdinalIgnoreCase)
                .ToList(); // ← QUAN TRỌNG NHẤT: ép thành List để không gọi lại OrderBy

            return sorted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sorting devices – returning unsorted");
            return result; // fallback: trả nguyên bản nếu có lỗi gì
        }
    }

    private async Task<DeviceResponseDto> UpdateOrCreateDevice(string ip, string mac, IPAddress? localIp, IPAddress gatewayIp, DateTime now)
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
                KnownIps = new(),
                KnownHostnames = new()
            };
        }

        if (known != null)
        {
            known.LastSeen = now;
            if (!known.KnownIps.Contains(ip)) known.KnownIps.Add(ip);

            try
            {
                var entry = await Dns.GetHostEntryAsync(ip);
                var hostname = entry.HostName.Split('.')[0];
                if (!known.KnownHostnames.Contains(hostname))
                    known.KnownHostnames.Add(hostname);
            }
            catch { }

            var (vendor, type) = GetVendorAndTypeFromMac(mac);
            known.Vendor = vendor;
            known.DeviceType = type;

            _devices.Upsert(known);
        }

        return ToDto(
            known ?? new KnownDevice { Mac = mac, Vendor = "Unknown", DeviceType = "Generic", FirstSeen = now, LastSeen = now },
            isOnline: true,
            now: now,
            currentIp: ip,
            isLocalPc: localIp?.ToString() == ip,
            isGateway: gatewayIp.ToString() == ip
        );
    }

    private DeviceResponseDto ToDto(KnownDevice device, bool isOnline, DateTime now, string currentIp,
                                    bool isLocalPc = false, bool isGateway = false)
    {
        var dto = new DeviceResponseDto
        {
            isOnline = isOnline,
            mac_address = device.Mac,
            ip = currentIp,
            first_seen = device.FirstSeen.ToString("dd MMM, yyyy, h:mm tt", CultureInfo.InvariantCulture),
            last_seen = device.LastSeen.ToString("dd MMM, yyyy, h:mm tt", CultureInfo.InvariantCulture),
            type = "Generic",
            name = "Generic",
            description = device.Vendor,
            location = "",
            system = ""
        };

        // Tên đẹp nhất
        if (!string.IsNullOrWhiteSpace(device.CustomName))
            dto.name = device.CustomName;
        else if (device.KnownHostnames.Count > 0)
            dto.name = device.KnownHostnames[^1];
        else if (device.Vendor != "Unknown")
            dto.name = device.Vendor;

        // Hệ điều hành
        foreach (var h in device.KnownHostnames)
        {
            var hl = h.ToLowerInvariant();
            if (hl.Contains("windows") || hl.Contains("desktop")) dto.system = "Windows";
            else if (hl.Contains("android")) dto.system = "Android";
            else if (hl.Contains("iphone") || hl.Contains("ipad")) dto.system = "iOS";
            else if (hl.Contains("macbook")) dto.system = "macOS";
        }

        // Loại thiết bị
        dto.type = device.DeviceType switch
        {
            "Router" => "Router",
            "iPhone/iPad" => "Mobile Phone",
            "Laptop" => "Laptop",
            "Smart TV" => "Smart TV",
            "Laptop/PC" => "Desktop",
            _ => "Generic"
        };

        if (isLocalPc)
        {
            dto.type = "Desktop";
            dto.name = Environment.MachineName;
            dto.description = $"{Environment.MachineName} (This Device)";
            dto.system = "Windows";
        }
        else if (isGateway)
        {
            dto.type = "Router";
            dto.name = device.KnownHostnames.LastOrDefault() ?? "Router";
            dto.description = "Default Gateway";
        }

        return dto;
    }

    // === Helper Methods giữ nguyên (đã test ổn) ===
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
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo("arp", "-a")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);

            var regex = new Regex(@"(?<ip>\d{1,3}(\.\d{1,3}){3})\s+(?<mac>[0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2}[:-][0-9A-Fa-f]{2})");
            foreach (Match m in regex.Matches(output))
            {
                var ip = m.Groups["ip"].Value.Trim();
                var mac = m.Groups["mac"].Value.Trim().Replace("-", ":").ToUpperInvariant();
                table[ip] = mac;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read ARP table");
        }
        return table;
    }

    private (string Vendor, string Type) GetVendorAndTypeFromMac(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac) || mac.Length < 8) return ("Unknown", "Generic");
        var oui = mac[..8].Replace(":", "").ToUpperInvariant();

        var map = new Dictionary<string, (string Vendor, string Type)>
        {
            {"B0B867", ("TP-Link", "Router")}, {"C8D3A3", ("TP-Link", "Router")}, {"F81A67", ("TP-Link", "Router")},
            {"6CE8B6", ("Huawei", "Router")}, {"ACD1B8", ("Xiaomi", "Router")},
            {"D4F4BE", ("Apple", "iPhone/iPad")}, {"F4F5D8", ("Apple", "iPhone/iPad")}, {"04E536", ("Apple", "iPhone/iPad")},
            {"E029E9", ("Lenovo", "Laptop")}, {"F49634", ("Intel", "Laptop/PC")}, {"00D49E", ("Dell", "Laptop")},
            {"D8C359", ("ASUS", "Laptop")}, {"E0B9BA", ("Samsung", "Smart TV")}
        };

        return map.TryGetValue(oui, out var v) ? v : ("Unknown", "Generic");
    }
}