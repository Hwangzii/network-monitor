// file: NetworkMonitor.Api/Services/Firewall/FirecallService.cs
using NetworkMonitor.Api.DTOs;
using NetworkMonitor.Api.Utils;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace NetworkMonitor.Api.Services.Firewall;

[SupportedOSPlatform("windows")]
public class FirewallService : IFirewallService
{
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
        // Lấy AppName từ AppId (ví dụ: "svchost_10.0.19041.1 (WinBuild.160101.0800)" -> "svchost")
        var appName = appId.Split('_').FirstOrDefault() ?? string.Empty;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainModule == null) continue;
                string currentAppName = Path.GetFileNameWithoutExtension(process.MainModule.FileName);

                if (currentAppName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                {
                    // Chuyển đổi Process thành DTO
                    processesList.Add(new FirewallProcessDto
                    {
                        ProcessName = $"{process.ProcessName}.exe",
                        ProcessID = process.Id,
                        // TODO: Lấy IconBase64 từ ứng dụng cha (FirewallAppDto) để nhất quán
                        IconBase64 = "", 
                        InConnections = "Allowed",
                        OutConnections = "Allowed",
                        // TODO: Logic lấy Hosts, Download/Upload Speed
                    });
                }
            }
            catch (Exception) { /* Bỏ qua lỗi truy cập */ }
            finally { process.Dispose(); }
        }

        return processesList;
    }

    // ====================================================================
    // Hàm Helper: Lấy danh sách Ứng dụng đang chạy
    // ====================================================================
    private List<FirewallAppDto> GetActiveApplications()
    {
        var appDict = new Dictionary<string, FirewallAppDto>();

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
                    string? iconBase64 = IconExtractor.GetBase64IconFromExe(exePath);
                    
                    appDict[appName] = new FirewallAppDto
                    {
                        AppId = $"{appName.Replace(" ", "_").ToLower()}_{version}", 
                        AppName = appName,
                        IconBase64 = iconBase64 ?? "",
                        Version = version,
                        InConnections = "Allowed", 
                        OutConnections = "Allowed",
                        // Các thuộc tính khác (Hosts, Speed, etc.) được giữ mặc định "" hoặc "0 B/s"
                    };
                }
            }
            catch (Exception) { /* Bỏ qua lỗi truy cập hoặc process đã tắt */ }
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