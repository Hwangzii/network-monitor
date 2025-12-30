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

    // Icon mặc định (duplicate từ service để controller dùng khi xử lý dữ liệu cũ)
    private const string DEFAULT_APP_ICON = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAEaSURBVFhH7ZTbCoJAEIaFCCKCCKJnLTpQVBdB14HQ00T0CqUP4AN41puJAVe92F3HRZegHfgQFvH7/1nQMmPmZ+Z8uYJOCm01vJe64PF8cZ+Ftho89DxPC8IAeZ73QpZlJWmattsAfsBavsk0yRsD3Ox7ST3A4uTC/OjC7ODCdO/AZOfAeOvAaPOB4foDg1UVwLZtIUmSqG2AIq9vgNcc5coBKHIWgNec0RhAdAUUOSJrjsRxrLYBihxBMa85QzkARY7ImjOkAURXQJEjKOY1Z0RRpLYBihyRNUe5cgCKHEEprzmjMYDoCqjImiNhGKptgApvA3V57wFkzbUGEMmDIGgfAKH84ShypQBdyn3fFwfQSaE1Y+bvx7K+efsbU5+Ow3MAAAAASUVORK5CYII=";

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

        // 1. Phân tích khoảng thời gian (5m, 3h, 24h)
        DateTime fromTime = DateTime.UtcNow;
        if (range.EndsWith("m") && int.TryParse(range[..^1], out int m))
            fromTime = fromTime.AddMinutes(-m);
        else if (range.EndsWith("h") && int.TryParse(range[..^1], out int h))
            fromTime = fromTime.AddHours(-h);
        else
            fromTime = fromTime.AddMinutes(-30); // Mặc định nếu range sai định dạng

        // 2. Lấy dữ liệu từ DB
        var summaries = await _dbContext.UsageSummaries
            .Where(u => u.Timestamp >= fromTime)
            .AsNoTracking()
            .ToListAsync();

        if (!summaries.Any()) return Ok(new TrafficUsageSummaryDto());

        // 3. Giải mã dữ liệu JSON
        var decodedData = summaries
            .Select(s => JsonSerializer.Deserialize<TrafficUsageSummaryDto>(s.JsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
            .Where(d => d != null)
            .Cast<TrafficUsageSummaryDto>()
            .ToList();

        if (!decodedData.Any()) return Ok(new TrafficUsageSummaryDto());

        // Tính Grand Total an toàn
        long grandTotalBytes = decodedData.Sum(d => d.Apps?.Sum(a => a.UsageBytes) ?? 0);

        // 4. Gộp toàn bộ dữ liệu (Apps, Hosts, Countries, TrafficTypes)
        var appsAgg = new Dictionary<string, AppUsageDto>();
        var hostsAgg = new Dictionary<string, HostUsageDto>();
        var typeAgg = new Dictionary<string, long>();
        var countriesAgg = new Dictionary<string, CountryUsageDto>();

        foreach (var data in decodedData)
        {
            // Gộp Apps
            if (data.Apps != null)
            {
                foreach (var app in data.Apps)
                {
                    if (!appsAgg.TryGetValue(app.Name, out var aDto)) 
                    {
                        aDto = app; 
                        appsAgg[app.Name] = aDto;
                    }
                    else 
                    {
                        aDto.UsageBytes += app.UsageBytes;
                    }
                }
            }

            // Gộp Hosts - XỬ LÝ APP OWNER ICON CHO DỮ LIỆU CŨ
            if (data.Hosts != null)
            {
                foreach (var host in data.Hosts)
                {
                    // Đảm bảo icon không bao giờ null (dữ liệu cũ có thể null)
                    host.AppOwnerIcon ??= DEFAULT_APP_ICON;

                    if (!hostsAgg.TryGetValue(host.Hostname, out var hDto)) 
                    {
                        hostsAgg[host.Hostname] = host;
                    }
                    else 
                    {
                        hDto.UsageBytes += host.UsageBytes;
                        // Ưu tiên icon "tốt hơn" (khác default) nếu có
                        if (hDto.AppOwnerIcon == DEFAULT_APP_ICON && host.AppOwnerIcon != DEFAULT_APP_ICON)
                            hDto.AppOwnerIcon = host.AppOwnerIcon;
                    }
                }
            }

            // Gộp Traffic Types
            if (data.TrafficTypes != null && data.Apps != null)
            {
                long snapshotTotal = data.Apps.Sum(a => a.UsageBytes);
                foreach (var type in data.TrafficTypes)
                {
                    long estimatedBytes = (long)(type.Percentage / 100.0 * snapshotTotal);
                    typeAgg[type.Type] = typeAgg.GetValueOrDefault(type.Type) + estimatedBytes;
                }
            }

            // Gộp Countries
            if (data.Countries != null)
            {
                foreach (var country in data.Countries)
                {
                    if (!countriesAgg.TryGetValue(country.CountryCode, out var cDto))
                    {
                        cDto = new CountryUsageDto
                        {
                            CountryCode = country.CountryCode,
                            CountryName = country.CountryName,
                            FlagUrl = country.FlagUrl,
                            UsageBytes = country.UsageBytes
                        };
                        countriesAgg[country.CountryCode] = cDto;
                    }
                    else
                    {
                        cDto.UsageBytes += country.UsageBytes;
                    }
                }
            }
        }

        // 5. Tạo kết quả tổng hợp
        var result = new TrafficUsageSummaryDto
        {
            Apps = appsAgg.Values
                .OrderByDescending(x => x.UsageBytes)
                .Take(20)
                .Select(a => { 
                    a.Usage = FormatBytes(a.UsageBytes); 
                    return a; 
                })
                .ToList(),

            Hosts = hostsAgg.Values
                .OrderByDescending(x => x.UsageBytes)
                .Take(20)
                .Select(h => { 
                    h.Usage = FormatBytes(h.UsageBytes); 
                    return h; 
                })
                .ToList(),

            TrafficTypes = typeAgg
                .Select(kv => new TrafficTypeUsageDto 
                {
                    Type = kv.Key,
                    Usage = FormatBytes(kv.Value),
                    Percentage = grandTotalBytes > 0 ? Math.Round(kv.Value * 100.0 / grandTotalBytes, 1) : 0
                })
                .OrderByDescending(x => x.Percentage)
                .ToList(),

            Countries = countriesAgg.Values
                .OrderByDescending(c => c.UsageBytes)
                .Select(c => {
                    c.Usage = FormatBytes(c.UsageBytes);
                    return c;
                })
                .ToList()
        };

        return Ok(result);
    }

    private string FormatBytes(long bytes) => bytes <= 0 ? "0 B" : bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024.0:0.##} KB" : $"{bytes / 1048576.0:0.##} MB";
}