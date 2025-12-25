// file: NetworkMonitor.Api/Features/Traffic/Controllers/TrafficUsageController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using NetworkMonitor.Api.Features.Traffic.Services;
using System.Text.Json;

namespace NetworkMonitor.Api.Features.Traffic.Controllers;

[ApiController]
[Route("api/traffic")]
public class TrafficUsageController : ControllerBase
{
    private readonly TrafficUsageService _usageService;
    private readonly TrafficDbContext _dbContext;

    public TrafficUsageController(TrafficUsageService usageService, TrafficDbContext dbContext)
    {
        _usageService = usageService;
        _dbContext = dbContext;
    }

    [HttpGet("usage-summary")]
    public async Task<IActionResult> GetUsageSummary([FromQuery] string? range = null)
    {
        if (string.IsNullOrWhiteSpace(range))
            return Ok(_usageService.GetCurrentUsageSummary());

        var fromTime = DateTime.UtcNow.AddMinutes(-30); 

        var summaries = await _dbContext.UsageSummaries
            .Where(u => u.Timestamp >= fromTime)
            .AsNoTracking()
            .ToListAsync();

        if (!summaries.Any()) return Ok(new TrafficUsageSummaryDto());

        var decodedData = summaries
            .Select(s => JsonSerializer.Deserialize<TrafficUsageSummaryDto>(s.JsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
            .Where(d => d != null)
            .Cast<TrafficUsageSummaryDto>()
            .ToList();

        if (!decodedData.Any()) return Ok(new TrafficUsageSummaryDto());

        // Tính Grand Total an toàn
        long grandTotalBytes = decodedData.Sum(d => d.Apps?.Sum(a => a.UsageBytes) ?? 0);

        var appsAgg = new Dictionary<string, AppUsageDto>();
        var hostsAgg = new Dictionary<string, HostUsageDto>();
        var typeAgg = new Dictionary<string, long>();

        foreach (var data in decodedData)
        {
            // Agg Apps
            if (data.Apps != null)
            {
                foreach (var app in data.Apps)
                {
                    if (!appsAgg.TryGetValue(app.Name, out var aDto)) {
                        aDto = app; appsAgg[app.Name] = aDto;
                    }
                    else aDto.UsageBytes += app.UsageBytes;
                }
            }

            // Agg Hosts (Sửa từ == thành !=)
            if (data.Hosts != null)
            {
                foreach (var host in data.Hosts)
                {
                    if (!hostsAgg.TryGetValue(host.Hostname, out var hDto)) {
                        hDto = host; hostsAgg[host.Hostname] = hDto;
                    }
                    else hDto.UsageBytes += host.UsageBytes;
                }
            }

            // Agg Traffic Types (Sửa từ == thành !=)
            if (data.TrafficTypes != null && data.Apps != null)
            {
                long snapshotTotal = data.Apps.Sum(a => a.UsageBytes);
                foreach (var type in data.TrafficTypes)
                {
                    long estimatedBytes = (long)(type.Percentage / 100.0 * snapshotTotal);
                    typeAgg[type.Type] = typeAgg.GetValueOrDefault(type.Type) + estimatedBytes;
                }
            }
        }

        var result = new TrafficUsageSummaryDto
        {
            Apps = appsAgg.Values.OrderByDescending(x => x.UsageBytes).Take(20).Select(a => { a.Usage = FormatBytes(a.UsageBytes); return a; }).ToList(),
            Hosts = hostsAgg.Values.OrderByDescending(x => x.UsageBytes).Take(20).Select(h => { h.Usage = FormatBytes(h.UsageBytes); return h; }).ToList(),
            TrafficTypes = typeAgg.Select(kv => new TrafficTypeUsageDto {
                Type = kv.Key,
                Usage = FormatBytes(kv.Value),
                Percentage = grandTotalBytes > 0 ? Math.Round(kv.Value * 100.0 / grandTotalBytes, 1) : 0
            }).OrderByDescending(x => x.Percentage).ToList()
        };

        return Ok(result);
    }

    private string FormatBytes(long bytes) => bytes <= 0 ? "0 B" : bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024.0:0.##} KB" : $"{bytes / 1048576.0:0.##} MB";
}