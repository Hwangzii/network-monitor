// file: NetworkMonitor.Api/Features/Traffic/Services/TrafficChartService.cs
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using Microsoft.EntityFrameworkCore;

namespace NetworkMonitor.Api.Features.Traffic.Services;

public class TrafficChartService
{
    private readonly TrafficDbContext _db;

    public TrafficChartService(TrafficDbContext db)
    {
        _db = db;
    }

    public async Task<TrafficChartResponseDto> GetChartAsync(string range)
    {
        var (fromTime, bucketSeconds) = ResolveRange(range);

        var samples = await _db.TrafficSamples
            .Where(x => x.Time >= fromTime)
            .OrderBy(x => x.Time)
            .ToListAsync();

        var bucketTicks = TimeSpan.FromSeconds(bucketSeconds).Ticks;

        var points = samples
            .GroupBy(x =>
                new DateTime(
                    (x.Time.Ticks / bucketTicks) * bucketTicks,
                    DateTimeKind.Utc))
            .Select(g => new TrafficChartPointDto
            {
                Time = g.Key,
                Download = g.Average(x => x.DownloadSpeed),
                Upload   = g.Average(x => x.UploadSpeed)
            })
            .OrderBy(x => x.Time)
            .ToList();

        var maxValue = points
            .SelectMany(p => new[] { p.Download, p.Upload })
            .DefaultIfEmpty(0)
            .Max();

        return new TrafficChartResponseDto
        {
            Points = points,
            MaxY = AddPadding(maxValue)
        };
    }


    // ===============================
    private static (DateTime from, int bucketSeconds) ResolveRange(string range)
    {
        var now = DateTime.UtcNow;

        return range switch
        {
            "5m"  => (now.AddMinutes(-5),   2),    // raw sampler rate
            "3h"  => (now.AddHours(-3),     30),   // ~360 points
            "24h" => (now.AddHours(-24),    300),  // 5 minutes
            _     => (now.AddMinutes(-5),   2)
        };
    }


    private static double AddPadding(double max)
    {
        if (max <= 0) return 10;

        // cộng thêm 20% hoặc tối thiểu 10 KB/s
        var padding = Math.Max(max * 0.2, 10 * 1024);
        return max + padding;
    }
}
