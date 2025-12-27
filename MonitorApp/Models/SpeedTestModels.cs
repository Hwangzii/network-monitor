using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonitorApp.Models
{
    public sealed class SpeedEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("data")]
        public SpeedEventData? Data { get; set; }
    }

    public sealed class SpeedEventData
    {
        // CHỈ 1 bộ field -> không còn collide
        [JsonPropertyName("downloadMbps")]
        public double? DownloadMbps { get; set; }

        [JsonPropertyName("uploadMbps")]
        public double? UploadMbps { get; set; }

        [JsonPropertyName("pingMs")]
        public double? PingMs { get; set; }

        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("quality")]
        public string? Quality { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class SpeedHistoryResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<SpeedHistoryItem> Data { get; set; } = new();
    }

    public class SpeedHistoryItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("downloadMbps")]
        public double DownloadMbps { get; set; }

        [JsonPropertyName("uploadMbps")]
        public double UploadMbps { get; set; }

        [JsonPropertyName("pingMs")]
        public double PingMs { get; set; }
    }

    public class SpeedHistoryRow
    {
        public int Id { get; set; }
        public string Date { get; set; } = "";
        public double Download { get; set; }
        public double Upload { get; set; }
        public double Ping { get; set; }
    }
}
