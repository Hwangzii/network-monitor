// file: NetworkMonitor.Api/Features/Traffic/Services/TrafficUsageBackgroundService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.Models;
using Newtonsoft.Json;

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
            try
            {
                var summary = _usageService.GetCurrentUsageSummary();
                if (summary.Apps.Any() || summary.Hosts.Any() || summary.Countries.Any())
                {
                    var entry = new UsageSummary
                    {
                        Timestamp = DateTime.UtcNow,
                        JsonData = JsonConvert.SerializeObject(summary)
                    };
                    _dbContext.UsageSummaries.Add(entry);
                    await _dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Saved usage summary snapshot to DB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving usage summary snapshot");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Every 5s
        }
    }
}