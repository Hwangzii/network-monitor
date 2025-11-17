// // // NetworkMonitor.Api/DTOs/TrafficDto.cs
// // namespace NetworkMonitor.Api.DTOs;

// // public class TrafficSummaryDto
// // {
// //     public WanLanStats Wan { get; set; } = new();
// //     public WanLanStats Lan { get; set; } = new();
// //     public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
// // }

// // public class WanLanStats
// // {
// //     public long TotalUpload { get; set; }      // Bytes
// //     public long TotalDownload { get; set; }    // Bytes
// //     public double SpeedUpload { get; set; }    // Bytes/s
// //     public double SpeedDownload { get; set; }  // Bytes/s
// // }

// namespace NetworkMonitor.Api.DTOs;

// public class TrafficSummaryDto
// {
//     public NetworkStats Wan { get; set; } = new();
//     public NetworkStats Lan { get; set; } = new();
//     public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
// }

// public class NetworkStats
// {
//     // Tổng
//     public string TotalUpload { get; set; } = "0 B";      // "1.5 MB"
//     public string TotalDownload { get; set; } = "0 B";   // "1.1 MB"

//     // Tốc độ
//     public string SpeedUpload { get; set; } = "0 B/s";    // "30 B/s"
//     public string SpeedDownload { get; set; } = "0 B/s"; // "392 B/s"
// }