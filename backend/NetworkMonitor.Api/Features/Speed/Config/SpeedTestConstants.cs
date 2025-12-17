// Features/Speed/SpeedTestConstants.cs
namespace NetworkMonitor.Api.Features.Speed;

public static class SpeedTestConstants
{
    public const string RelativeExePath = @"Tools\Speedtest\speedtest.exe";

    public static class StatusMessages
    {
        public const string Initializing = "Initializing speed test...";
        public const string FindingServer = "Finding the best server...";
        public const string MeasuringPing = "Measuring ping...";
        public const string TestingBandwidth = "Testing download & upload (this may take 15-30 seconds)...";
        public const string TestCompleted = "Test completed. Displaying results...";
    }

    public static class QualityThresholds
    {
        // Ping (ms)
        public const double PingExcellent = 20;
        public const double PingGood = 50;
        public const double PingFair = 100;

        // Download (Mbps)
        public const double DownloadExcellent = 100;
        public const double DownloadGood = 50;
        public const double DownloadFair = 25;

        // Upload (Mbps)
        public const double UploadExcellent = 50;
        public const double UploadGood = 20;
        public const double UploadFair = 10;
    }
}