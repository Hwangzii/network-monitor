// Models/TrafficSummary.cs
namespace MonitorApp.Models
{
    public class TrafficSummary
    {
        public string TotalUsage { get; set; } = "0 MB";
        public string DownloadTotal { get; set; } = "0 MB";
        public string DownloadSpeed { get; set; } = "↓ 0 B/s";
        public string UploadTotal { get; set; } = "0 MB";
        public string UploadSpeed { get; set; } = "↑ 0 B/s";
        public string WanUsage { get; set; } = "0 MB";
        public string LanUsage { get; set; } = "0 MB";
        public double DownloadRatio { get; set; } = 0;
        public double UploadRatio { get; set; } = 0;
        public string UpdatedAt { get; set; } = "";
    }
}