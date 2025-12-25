// file: NetworkMonitor.Api/Features/Traffic/Services/TrafficUsageBackgroundService.cs
using System.Text.Json; // QUAN TRỌNG: Đổi từ Newtonsoft.Json sang System.Text.Json
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.Models;

namespace NetworkMonitor.Api.Features.Traffic.Services;

public class TrafficUsageBackgroundService : BackgroundService
{
    private readonly TrafficUsageService _usageService;
    private readonly TrafficDbContext _dbContext;
    private readonly ILogger<TrafficUsageBackgroundService> _logger;

    public TrafficUsageBackgroundService(
        TrafficUsageService usageService,
        TrafficDbContext dbContext,
        ILogger<TrafficUsageBackgroundService> logger)
    {
        _usageService = usageService;
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            try
            {
                var summary = _usageService.GetCurrentUsageSummary();
                if (summary.Apps != null && summary.Apps.Any())
                {
                    var entry = new UsageSummary
                    {
                        Timestamp = DateTime.UtcNow,
                        // Bây giờ hàm này sẽ được hiểu đúng từ thư viện System.Text.Json
                        JsonData = JsonSerializer.Serialize(summary) 
                    };
                    _dbContext.UsageSummaries.Add(entry);
                    await _dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error saving summary"); }
        }
    }
}