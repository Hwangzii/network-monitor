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
using NetworkMonitor.Api.Features.Scanner.Config;

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
        // var result = new List<DeviceResponseDto>();

        // TẠO DANH SÁCH CÁC TASK ĐỂ CHẠY SONG SONG
        var tasks = activeIps.Select(ip => 
            UpdateOrCreateDevice(ip.ToString(), arpTable.GetValueOrDefault(ip.ToString(), "N/A"), localIp, gatewayIp, now)
        );

        // Chạy tất cả cùng lúc
        var devices = await Task.WhenAll(tasks);
        var result = devices.ToList();

        foreach (var d in result)
        {
            if (!string.IsNullOrEmpty(d.mac_address) && d.mac_address != "N/A")
                onlineMacs.Add(d.mac_address);
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
                // Giới hạn thời gian chờ DNS tối đa 1 giây
                var dnsTask = Dns.GetHostEntryAsync(ip);
                if (await Task.WhenAny(dnsTask, Task.Delay(1000)) == dnsTask)
                {
                    var entry = await dnsTask;
                    var hostname = entry.HostName.Split('.')[0];
                    if (!known.KnownHostnames.Contains(hostname))
                        known.KnownHostnames.Add(hostname);
                }
            }
            catch { /* Bỏ qua nếu lỗi hoặc timeout */ }

            _devices.Upsert(known);
        }

        // Tạo DTO
        var dto = ToDto(
            known ?? new KnownDevice { Mac = mac, Vendor = "Unknown", DeviceType = "Generic", FirstSeen = now, LastSeen = now },
            isOnline: true,
            now: now,
            currentIp: ip,
            isLocalPc: localIp?.ToString() == ip,
            isGateway: gatewayIp.ToString() == ip
        );

        // THÊM: Scan ports chỉ khi online và có IP hợp lệ
        if (!string.IsNullOrWhiteSpace(ip) && ip != "N/A")
        {
            dto.ports = await ScanOpenPortsAsync(ip);
        }
        else
        {
            dto.ports = "";
        }

        return dto;
    }

    private DeviceResponseDto ToDto(KnownDevice device, bool isOnline, DateTime now, string currentIp,
                                bool isLocalPc = false, bool isGateway = false)
    {
        // 1. Lấy Vendor và Type cơ bản từ file Config dựa trên MAC
        var (vendor, type) = DeviceLookup.GetVendorInfo(device.Mac);

        var dto = new DeviceResponseDto
        {
            isOnline = isOnline,
            mac_address = device.Mac,
            ip = currentIp,
            first_seen = device.FirstSeen.ToString("dd MMM, yyyy, h:mm tt", CultureInfo.InvariantCulture),
            last_seen = device.LastSeen.ToString("dd MMM, yyyy, h:mm tt", CultureInfo.InvariantCulture),
            type = type,        // Lấy từ lookup
            description = vendor, // Lấy từ lookup
            location = "",
            system = ""
        };

        // 2. Ưu tiên xác định Tên thiết bị (Name)
        if (!string.IsNullOrWhiteSpace(device.CustomName))
            dto.name = device.CustomName;
        else if (device.KnownHostnames.Count > 0)
            dto.name = device.KnownHostnames[^1];
        else if (vendor != "Unknown")
            dto.name = vendor;
        else
            dto.name = "Generic Device";

        // 3. Nhận diện Hệ điều hành (System) qua Hostname
        foreach (var h in device.KnownHostnames)
        {
            var hl = h.ToLowerInvariant();
            if (hl.Contains("windows") || hl.Contains("desktop")) dto.system = "Windows";
            else if (hl.Contains("android")) dto.system = "Android";
            else if (hl.Contains("iphone") || hl.Contains("ipad")) dto.system = "iOS";
            else if (hl.Contains("macbook")) dto.system = "macOS";
        }

        // 4. Gán Icon dựa trên Type đã phân loại từ DeviceLookup
        dto.iconDeviceUrl = DeviceLookup.GetIconUrl(dto.type);

        // 5. Xử lý các trường hợp đặc biệt (Override cho máy này và Gateway)
        if (isLocalPc)
        {
            dto.type = "Desktop";
            dto.name = Environment.MachineName;
            dto.description = $"{Environment.MachineName} (This Device)";
            dto.system = "Windows";
            dto.iconDeviceUrl = DeviceLookup.GetIconUrl("Desktop"); //
        }
        else if (isGateway)
        {
            dto.type = "Router";
            dto.name = device.KnownHostnames.LastOrDefault() ?? "Router";
            dto.description = "Default Gateway";
            dto.iconDeviceUrl = DeviceLookup.GetIconUrl("Router"); //
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

        // Danh sách các port phổ biến cần scan (phù hợp với router, camera, printer, server, IoT...)
    private static readonly int[] CommonPorts = new[]
    {
        21,    // FTP
        22,    // SSH
        23,    // Telnet
        80,    // HTTP
        81,    // Alt HTTP
        443,   // HTTPS
        8080,  // Alt HTTP
        8443,  // Alt HTTPS
        554,   // RTSP (camera)
        8554,  // RTSP alt
        37777, // DVR / IP Camera
        5000,  // UPnP / Docker
        1900,  // SSDP / UPnP
        9100   // Printer
    };

    // Hàm scan port nhanh (timeout 800ms/port)
        // Hàm scan port nhanh với timeout đúng cách
    private async Task<string> ScanOpenPortsAsync(string ipStr)
    {
        if (string.IsNullOrWhiteSpace(ipStr) || ipStr == "N/A" || !IPAddress.TryParse(ipStr, out var ip))
            return "";

        var openPorts = new List<int>();
        // Giảm timeout xuống 200-300ms cho mạng nội bộ
        var timeoutMs = 250; 

        // Chỉ quét các port thực sự quan trọng để nhận diện loại thiết bị
        var essentialPorts = new[] { 80, 443, 22, 135, 445 }; 

        var tasks = essentialPorts.Select(port => Task.Run(async () =>
        {
            using var client = new TcpClient();
            try
            {
                var connectTask = client.ConnectAsync(ip, port);
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));

                if (completedTask == connectTask && client.Connected)
                {
                    lock (openPorts) { openPorts.Add(port); }
                }
            }
            catch { }
        }));

        await Task.WhenAll(tasks);
        return openPorts.Count == 0 ? "" : string.Join(",", openPorts.OrderBy(x => x));
    }

    private Dictionary<string, string> GetArpTable()
    {
        var arpTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Regex để bắt IP và MAC address từ output của lệnh arp -a
            var regex = new Regex(@"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+([0-9a-f]{2}[:-][0-9a-f]{2}[:-][0-9a-f]{2}[:-][0-9a-f]{2}[:-][0-9a-f]{2}[:-][0-9a-f]{2})", RegexOptions.IgnoreCase);
            var matches = regex.Matches(output);

            foreach (Match match in matches)
            {
                var ip = match.Groups[1].Value;
                var mac = match.Groups[2].Value.Replace("-", ":").ToUpperInvariant();
                if (!arpTable.ContainsKey(ip)) arpTable[ip] = mac;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading ARP table");
        }
        return arpTable;
    }
}