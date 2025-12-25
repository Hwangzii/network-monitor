// file: NetworkMonitor.Api/Features/Traffic/Controllers/TrafficUsageController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.DTOs;
using NetworkMonitor.Api.Features.Traffic.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitor.Api.Features.Traffic.Controllers;

[ApiController]
[Route("api/traffic")]
public class TrafficUsageController : ControllerBase
{
    private readonly TrafficUsageService _usageService;
    private readonly TrafficDbContext _dbContext;

    public TrafficUsageController(
        TrafficUsageService usageService,
        TrafficDbContext dbContext)
    {
        _usageService = usageService;
        _dbContext = dbContext;
    }

    [HttpGet("usage-summary")]
    public async Task<IActionResult> GetUsageSummary([FromQuery] string? range = null)
    {
        // =========================
        // REALTIME
        // =========================
        if (string.IsNullOrWhiteSpace(range))
            return Ok(_usageService.GetCurrentUsageSummary());

        // =========================
        // PARSE RANGE
        // =========================
        TimeSpan timeSpan;
        if (range.EndsWith("m") && int.TryParse(range[..^1], out int minutes))
        {
            timeSpan = TimeSpan.FromMinutes(minutes);
        }
        else if (range.EndsWith("h") && int.TryParse(range[..^1], out int hours))
        {
            timeSpan = TimeSpan.FromHours(hours);
        }
        else
        {
            return BadRequest("Invalid range format. Use '5m', '3h', '24h'.");
        }

        var fromTime = DateTime.UtcNow - timeSpan;

        var summaries = await _dbContext.UsageSummaries
            .Where(u => u.Timestamp >= fromTime)
            .OrderBy(u => u.Timestamp)
            .ToListAsync();

        if (!summaries.Any())
            return Ok(new TrafficUsageSummaryDto());

        // =========================
        // AGGREGATION BUFFERS
        // =========================
        var apps = new Dictionary<string, AppUsageDto>();
        var hosts = new Dictionary<string, HostUsageDto>();
        var countries = new Dictionary<string, long>(); // countryCode -> bytes
        var trafficTypes = new Dictionary<string, long>(); // type -> bytes

        long totalBytesAll = 0;

        // =========================
        // AGGREGATE RAW DATA
        // =========================
        foreach (var row in summaries)
        {
            var data = JsonConvert.DeserializeObject<TrafficUsageSummaryDto>(row.JsonData);
            if (data == null) continue;

            // -------- APPS (SOURCE OF TRUTH) --------
            foreach (var app in data.Apps)
            {
                if (!apps.TryGetValue(app.Name, out var agg))
                {
                    agg = new AppUsageDto
                    {
                        Name = app.Name,
                        AppIcon = app.AppIcon,
                        CountryName = app.CountryName,
                        CountryCode = app.CountryCode,
                        CountryFlagUrl = app.CountryFlagUrl
                    };
                    apps[app.Name] = agg;
                }

                agg.UsageBytes += app.UsageBytes;
                totalBytesAll += app.UsageBytes;
            }

            // -------- HOSTS --------
            foreach (var host in data.Hosts)
            {
                if (!hosts.TryGetValue(host.Hostname, out var agg))
                {
                    agg = new HostUsageDto
                    {
                        Hostname = host.Hostname,
                        CountryName = host.CountryName,
                        CountryCode = host.CountryCode,
                        CountryFlagUrl = host.CountryFlagUrl
                    };
                    hosts[host.Hostname] = agg;
                }

                agg.UsageBytes += host.UsageBytes;

                if (!string.IsNullOrWhiteSpace(host.CountryCode))
                {
                    countries[host.CountryCode] =
                        countries.GetValueOrDefault(host.CountryCode) + host.UsageBytes;
                }
            }

            // -------- TRAFFIC TYPES (ESTIMATED) --------
            foreach (var type in data.TrafficTypes)
            {
                var estimatedBytes = (long)(type.Percentage / 100.0 * totalBytesAll);
                trafficTypes[type.Type] =
                    trafficTypes.GetValueOrDefault(type.Type) + estimatedBytes;
            }
        }

        // =========================
        // BUILD FINAL DTO
        // =========================
        var result = new TrafficUsageSummaryDto
        {
            Apps = apps.Values
                .OrderByDescending(a => a.UsageBytes)
                .Take(20)
                .Select(a =>
                {
                    a.Usage = FormatBytes(a.UsageBytes);
                    return a;
                })
                .ToList(),

            Hosts = hosts.Values
                .OrderByDescending(h => h.UsageBytes)
                .Take(20)
                .Select(h =>
                {
                    h.Usage = FormatBytes(h.UsageBytes);
                    return h;
                })
                .ToList(),

            TrafficTypes = trafficTypes
                .Select(kv => new TrafficTypeUsageDto
                {
                    Type = kv.Key,
                    Usage = FormatBytes(kv.Value),
                    Percentage = totalBytesAll > 0
                        ? Math.Round(kv.Value * 100.0 / totalBytesAll, 1)
                        : 0
                })
                .OrderByDescending(t => t.Percentage)
                .ToList(),

            Countries = countries
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new CountryUsageDto
                {
                    CountryCode = kv.Key,
                    CountryName = kv.Key.ToUpper(),
                    Usage = FormatBytes(kv.Value),
                    FlagUrl = $"https://flagcdn.com/w40/{kv.Key}.png"
                })
                .ToList()
        };

        return Ok(result);
    }

    // =========================
    // HELPERS
    // =========================
    private string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";

        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:0.##} KB";
        if (bytes < 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):0.##} MB";

        return $"{bytes / (1024.0 * 1024 * 1024):0.##} GB";
    }
}
