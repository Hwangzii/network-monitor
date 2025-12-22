// NetworkMonitor.Api/Features/Traffic/Services/TrafficSampler.cs
using System.Net.NetworkInformation;

namespace NetworkMonitor.Api.Features.Traffic.Services;

public class TrafficSampler
{
    private static long _prevDown;
    private static long _prevUp;
    private static DateTime _lastTime = DateTime.MinValue;

    public (double Down, double Up) SampleSpeed()
    {
        var ni = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.GetIPProperties().GatewayAddresses.Any());

        if (ni == null)
            return (0, 0);

        var stats = ni.GetIPStatistics();
        var now = DateTime.UtcNow;

        double down = 0, up = 0;

        if (_lastTime != DateTime.MinValue)
        {
            var seconds = (now - _lastTime).TotalSeconds;
            if (seconds > 0)
            {
                down = (stats.BytesReceived - _prevDown) / seconds;
                up   = (stats.BytesSent     - _prevUp)   / seconds;
            }
        }

        _prevDown = stats.BytesReceived;
        _prevUp   = stats.BytesSent;
        _lastTime = now;

        return (Math.Max(0, down), Math.Max(0, up));
    }
}
