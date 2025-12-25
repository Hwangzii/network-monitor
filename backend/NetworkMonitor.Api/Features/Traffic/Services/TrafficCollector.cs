// NetworkMonitor.Api/Features/Traffic/Services/TrafficCollector.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.Models;

namespace NetworkMonitor.Api.Features.Traffic.Services;

public class TrafficCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TrafficSampler _sampler;

    public TrafficCollector(
        IServiceScopeFactory scopeFactory,
        TrafficSampler sampler)
    {
        _scopeFactory = scopeFactory;
        _sampler = sampler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);

        Console.WriteLine("[TrafficCollector] running...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _sampler.SampleSpeed();
            double down = result.Down;
            double up   = result.Up;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TrafficDbContext>();

            db.TrafficSamples.Add(new TrafficSample
            {
                Time = DateTime.UtcNow,
                DownloadSpeed = down,
                UploadSpeed = up
            });

            await db.SaveChangesAsync(stoppingToken);

            Console.WriteLine(
                $"[TrafficCollector] saved ↓ {down / 1024:0.##} KB/s | ↑ {up / 1024:0.##} KB/s");

            await Task.Delay(2000, stoppingToken);
        }
    }
}
