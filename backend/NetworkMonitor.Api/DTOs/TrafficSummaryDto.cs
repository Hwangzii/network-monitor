// file: NetworkMonitor.Api/DTOs/TrafficSummaryDto.cs
namespace NetworkMonitor.Api.DTOs;

public class TrafficSummaryDto
{
    // TỔNG QUAN
    public string TotalUsage { get; set; } = "0 B";           // 2.0 MB

    // TẢI XUỐNG
    public string DownloadTotal { get; set; } = "0 B";        // 706.5 KB
    public string DownloadSpeed { get; set; } = "0 B/s";      // 4 KB/s ↓

    // TẢI LÊN
    public string UploadTotal { get; set; } = "0 B";          // 1.3 MB
    public string UploadSpeed { get; set; } = "0 B/s";        // 5 KB/s ↑

    // PHÂN LOẠI KẾT NỐI
    public string WanUsage { get; set; } = "0 B";             // 2.0 MB
    public string LanUsage { get; set; } = "0 B";             // 21.8 KB

    // BỔ SUNG: TỈ LỆ THANH ĐO (FRONTEND DÙNG NGAY)
    public double DownloadRatio { get; set; } = 0;            // 80
    public double UploadRatio { get; set; } = 0;              // 20

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}