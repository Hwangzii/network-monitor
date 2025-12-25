// file: NetworkMonitor.Api/Services/NetworkMonitor/EtwNetworkTrafficMonitorHostedService.cs
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;

namespace NetworkMonitor.Api.Services.NetworkMonitor;

[SupportedOSPlatform("windows")]
public class EtwNetworkTrafficMonitorHostedService : IHostedService
{
    private EtwNetworkTrafficMonitor? _monitor;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _monitor = new EtwNetworkTrafficMonitor();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _monitor?.Dispose();
        return Task.CompletedTask;
    }
}