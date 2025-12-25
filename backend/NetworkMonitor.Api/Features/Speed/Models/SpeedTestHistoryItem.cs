// Features/Speed/Models/SpeedTestHistoryItem.cs
namespace NetworkMonitor.Api.Features.Speed.Models;

public class SpeedTestHistoryItem
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double DownloadMbps { get; set; }
    public double UploadMbps { get; set; }
    public double PingMs { get; set; }
}