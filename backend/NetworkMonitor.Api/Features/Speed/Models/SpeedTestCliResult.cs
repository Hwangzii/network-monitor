// Features/Speed/Models/SpeedTestCliResult.cs
using System.Text.Json.Serialization;

namespace NetworkMonitor.Api.Features.Speed.Models;

public class SpeedTestCliResult
{
    public DownloadInfo Download { get; set; } = null!;
    public UploadInfo Upload { get; set; } = null!;
    public PingInfo Ping { get; set; } = null!;
    public string Isp { get; set; } = null!;
    public InterfaceInfo Interface { get; set; } = null!;
    public ServerInfo Server { get; set; } = null!;

    public class DownloadInfo { public double Bandwidth { get; set; } }
    public class UploadInfo { public double Bandwidth { get; set; } }
    public class PingInfo { [JsonPropertyName("latency")] public double Latency { get; set; } }
    public class InterfaceInfo { [JsonPropertyName("externalIp")] public string ExternalIp { get; set; } = null!; }
    public class ServerInfo { public string Name { get; set; } = null!; public string Country { get; set; } = null!; }
}