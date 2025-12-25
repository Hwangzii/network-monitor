using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace MonitorApp.Models
{
    public class TrafficChartPoint
    {
        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("download")]
        public double Download { get; set; }

        [JsonPropertyName("upload")]
        public double Upload { get; set; }
    }

    public class TrafficChartResponse
    {
        [JsonPropertyName("points")]
        public List<TrafficChartPoint> Points { get; set; } = new();

        [JsonPropertyName("maxY")]
        public double MaxY { get; set; }
    }
}

