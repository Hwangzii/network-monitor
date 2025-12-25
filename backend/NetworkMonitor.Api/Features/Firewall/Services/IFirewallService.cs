// file: NetworkMonitor.Api/Services/Firewall/IFirewallService.cs
using NetworkMonitor.Api.DTOs;

namespace NetworkMonitor.Api.Services.Firewall;

public interface IFirewallService
{
    // API 1: Lấy danh sách ứng dụng, có lọc và phân trang
    Task<PagedResponseDto<FirewallAppDto>> GetFirewallAppsAsync(
        string status, 
        int page, 
        int limit);

    // BỔ SUNG: Định nghĩa cho API 2 (Processes con)
    Task<List<FirewallProcessDto>> GetAppProcessesAsync(string appId); 
}