// file: NetworkMonitor.Api/Services/Firewall/FirewallService.cs (đã sửa)
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Utils;
using NetworkMonitor.Api.Services.NetworkMonitor;
using NetworkMonitor.Api.Services.NetworkMonitor.Models;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace NetworkMonitor.Api.Services.Firewall;

[SupportedOSPlatform("windows")]
public class FirewallService : IFirewallService
{
    private readonly INetworkStatusChecker _statusChecker;
    private readonly INetworkTrafficMonitor _trafficMonitor;  // <--- MỚI

    public FirewallService(INetworkStatusChecker statusChecker, INetworkTrafficMonitor trafficMonitor)
    {
        _statusChecker = statusChecker;
        _trafficMonitor = trafficMonitor;

        if (_statusChecker is NetworkStatusChecker checker)
            checker.LoadOnce();
    }

    // ====================================================================
    // API 1: Lấy danh sách Ứng dụng (Apps)
    // ====================================================================
    public async Task<PagedResponseDto<FirewallAppDto>> GetFirewallAppsAsync(
        string status, 
        int page, 
        int limit)
    {
        // Giả định: hiện tại chỉ triển khai logic cho "active"
        var allApps = status.Equals("active", StringComparison.OrdinalIgnoreCase) 
            ? GetActiveApplications()
            : new List<FirewallAppDto>();

        // 1. Phân trang
        var totalRecords = allApps.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / limit);
        
        var pagedApps = allApps
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        // 2. Xây dựng Response DTO
        return new PagedResponseDto<FirewallAppDto>
        {
            Pagination = new PagingInfo
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = page,
                Limit = limit
            },
            Data = pagedApps
        };
    }

    // ====================================================================
    // API 2: Lấy danh sách Processes con theo AppId
    // ====================================================================
    public async Task<List<FirewallProcessDto>> GetAppProcessesAsync(string appId)
    {
        var processesList = new List<FirewallProcessDto>();
        var appName = appId.Split('_').FirstOrDefault() ?? string.Empty;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainModule == null) continue;
                string exePath = process.MainModule.FileName;
                string currentAppName = Path.GetFileNameWithoutExtension(exePath);

                if (currentAppName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                {
                    string inStatus = _statusChecker.CheckInboundStatus(exePath);
                    string outStatus = _statusChecker.CheckOutboundStatus(exePath);

                    var usage = _trafficMonitor.GetUsageByPid(process.Id);                // <--- MỚI
                    var hosts = _trafficMonitor.GetHostsByPid(process.Id);                // <--- MỚI

                    string? iconBase64 = IconExtractor.GetBase64IconFromExe(exePath);

                    processesList.Add(new FirewallProcessDto
                    {
                        ProcessName = $"{process.ProcessName}.exe",
                        ProcessID = process.Id,
                        IconBase64 = iconBase64 ?? "",
                        InConnections = inStatus,
                        OutConnections = outStatus,
                        Hosts = string.Join(", ", hosts.Select(h => h.Domain ?? h.RemoteIp)),
                        DownloadSpeed = FormatHelper.FormatSpeed(usage.DownloadBytesPerSecond),
                        UploadSpeed = FormatHelper.FormatSpeed(usage.UploadBytesPerSecond),
                        // TODO: các field khác nếu cần
                    });
                }
            }
            catch (Exception) { /* Bỏ qua */ }
            finally { process.Dispose(); }
        }

        return processesList;
    }

    // ====================================================================
    // Hàm Helper: Lấy danh sách Ứng dụng đang chạy
    // ====================================================================
    private List<FirewallAppDto> GetActiveApplications()
    {
        var appDict = new Dictionary<string, FirewallAppDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainModule == null) continue;

                string exePath = process.MainModule.FileName;
                string appName = Path.GetFileNameWithoutExtension(exePath);

                if (!appDict.ContainsKey(appName))
                {
                    string version = GetVersion(exePath);
                    string appId = $"{appName.Replace(" ", "_").ToLower()}_{version}";

                    string inStatus = _statusChecker.CheckInboundStatus(exePath);
                    string outStatus = _statusChecker.CheckOutboundStatus(exePath);

                    var usage = _trafficMonitor.GetUsageByPid(process.Id);           // <--- MỚI
                    var hosts = _trafficMonitor.GetHostsByPid(process.Id);           // <--- MỚI

                    string? iconBase64 = IconExtractor.GetBase64IconFromExe(exePath);

                    appDict[appName] = new FirewallAppDto
                    {
                        AppId = appId,
                        AppName = appName,
                        IconBase64 = iconBase64 ?? "",
                        Version = version,
                        InConnections = inStatus,
                        OutConnections = outStatus,
                        Hosts = string.Join(", ", hosts.Select(h => h.Domain ?? h.RemoteIp)),
                        DownloadSpeed = FormatHelper.FormatSpeed(usage.DownloadBytesPerSecond),
                        UploadSpeed = FormatHelper.FormatSpeed(usage.UploadBytesPerSecond),
                        VisualTotal = "",
                        VirusTotal = ""
                    };
                }
            }
            catch (Exception) { /* Bỏ qua */ }
            finally { process.Dispose(); }
        }

        return appDict.Values.ToList();
    }

    private string GetVersion(string exePath)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            return versionInfo.FileVersion ?? versionInfo.ProductVersion ?? "N/A";
        }
        catch { return "N/A"; }
    }
}