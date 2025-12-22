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

        // ===== GROUP BY TIME BUCKET =====
        var points = samples
            .GroupBy(x =>
                new DateTime(
                    (x.Time.Ticks / TimeSpan.FromSeconds(bucketSeconds).Ticks)
                    * TimeSpan.FromSeconds(bucketSeconds).Ticks,
                    DateTimeKind.Utc))
            .Select(g =>
            {
                var peakSample = g
                    .OrderByDescending(x => x.DownloadSpeed + x.UploadSpeed)
                    .First();

                return new TrafficChartPointDto
                {
                    Time = g.Key,
                    Download = peakSample.DownloadSpeed,
                    Upload   = peakSample.UploadSpeed
                };
            })
            .OrderBy(x => x.Time)
            .ToList();



        // ===== MAX Y + PADDING =====
        var maxValue = points
            .SelectMany(p => new[] { p.Download, p.Upload })
            .DefaultIfEmpty(0)
            .Max();

        var paddedMax = AddPadding(maxValue);

        return new TrafficChartResponseDto
        {
            Points = points,
            MaxY = paddedMax
        };
    }

    // ===============================
    private static (DateTime from, int bucketSeconds) ResolveRange(string range)
    {
        var now = DateTime.UtcNow;

        return range switch
        {
            "5m"  => (now.AddMinutes(-5),  20),
            "3h"  => (now.AddHours(-3),    600),
            "24h" => (now.AddHours(-24),   3600),
            _     => (now.AddMinutes(-5),  20)
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
