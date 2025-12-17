// file: NetworkMonitor.Api/Services/Firewall/NetworkStatusChecker.cs (đã sửa)
using System.Runtime.Versioning;
using System.Management; // Cần using System.Management
using System.Collections.Generic;

namespace NetworkMonitor.Api.Services.Firewall;

[SupportedOSPlatform("windows")]
public class NetworkStatusChecker : INetworkStatusChecker
{
    private readonly Dictionary<string, (bool inBlocked, bool outBlocked)> _ruleCache
        = new(StringComparer.OrdinalIgnoreCase);

    private bool _loaded = false;

    public void LoadOnce()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            var scope = new ManagementScope(@"\\.\root\StandardCimv2");

            // Kết nối với quyền hiện tại
            var options = new ConnectionOptions
            {
                Impersonation = ImpersonationLevel.Impersonate,
                EnablePrivileges = true
            };
            scope.Options = options;
            scope.Connect();

            var query = new ObjectQuery(
                "SELECT Program, Direction, Action FROM MSFT_NetFirewallApplicationFilter"
            );

            using var searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject obj in searcher.Get())
            {
                try
                {
                    string program = obj["Program"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(program)) continue;

                    int direction = Convert.ToInt32(obj["Direction"]);
                    int action = Convert.ToInt32(obj["Action"]);

                    if (!_ruleCache.ContainsKey(program))
                        _ruleCache[program] = (false, false);

                    var entry = _ruleCache[program];

                    if (action == 1) // Block
                    {
                        if (direction == 1) entry.inBlocked = true; // Inbound
                        if (direction == 2) entry.outBlocked = true; // Outbound
                    }

                    _ruleCache[program] = entry;
                }
                catch { /* Bỏ qua lỗi cho từng object */ }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Không có quyền truy cập WMI -> coi như không block
        }
        catch (ManagementException ex)
        {
            Console.WriteLine($"WMI Error during load: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading firewall rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Kiểm tra trạng thái kết nối vào (Inbound)
    /// </summary>
    public string CheckInboundStatus(string exePath)
    {
        LoadOnce(); // Đảm bảo load nếu chưa
        return _ruleCache.TryGetValue(exePath, out var r) && r.inBlocked
            ? "Blocked"
            : "Allowed";
    }

    /// <summary>
    /// Kiểm tra trạng thái kết nối ra (Outbound)
    /// </summary>
    public string CheckOutboundStatus(string exePath)
    {
        LoadOnce(); // Đảm bảo load nếu chưa
        return _ruleCache.TryGetValue(exePath, out var r) && r.outBlocked
            ? "Blocked"
            : "Allowed";
    }
}