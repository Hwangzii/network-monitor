// file : Features/Speed/DTOs/SpeedTestResponseDto.cs
namespace NetworkMonitor.Api.Features.Speed.DTOs;

public class SpeedTestResponseDto
{
    public double DownloadMbps { get; set; }
    public double UploadMbps { get; set; }
    public double PingMs { get; set; }
    public string? IpAddress { get; set; }
    public string? Provider { get; set; }
    public string? Location { get; set; }
}