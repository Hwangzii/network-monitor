namespace NetworkMonitor.Api.Features.Traffic.DTOs;

public class TrafficChartPointDto
{
    public DateTime Time { get; set; }
    public double Download { get; set; }
    public double Upload { get; set; }
}
