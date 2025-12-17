// Features/Speed/DTOs/SpeedTestHistoryResponseDto.cs
using NetworkMonitor.Api.Features.Speed.Models;

namespace NetworkMonitor.Api.Features.Speed.DTOs;

public class SpeedTestHistoryResponseDto
{
    public bool Success { get; set; } = true;
    public int Total { get; set; }
    public List<SpeedTestHistoryItem> Data { get; set; } = new();
}