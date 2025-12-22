namespace NetworkMonitor.Api.Features.Traffic.DTOs;

public class TrafficChartResponseDto
{
    public List<TrafficChartPointDto> Points { get; set; } = [];
    public double MaxY { get; set; }
}
