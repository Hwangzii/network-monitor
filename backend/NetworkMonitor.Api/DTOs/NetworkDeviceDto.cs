// File: NetworkMonitor.Api/DTOs/NetworkDeviceDto.cs (thay toàn bộ)
namespace NetworkMonitor.Api.DTOs;

public class NetworkDeviceDto
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string System { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Mac { get; set; } = "";
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    
    // MỚI: Trạng thái hiện tại
    public bool IsOnline { get; set; }
    public string LastSeenText { get; set; } = ""; // "5 phút trước", "hôm qua", "1 tuần trước"...
}