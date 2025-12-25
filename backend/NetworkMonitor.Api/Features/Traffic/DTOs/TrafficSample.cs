namespace NetworkMonitor.Api.Features.Traffic.Models;

public class TrafficSample
{
    public long Id { get; set; }

    public DateTime Time { get; set; }

    // Bytes / second
    public double DownloadSpeed { get; set; }
    public double UploadSpeed { get; set; }
}
