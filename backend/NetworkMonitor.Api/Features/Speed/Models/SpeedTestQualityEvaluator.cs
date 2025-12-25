// Features/Speed/Models/SpeedTestQualityEvaluator.cs
namespace NetworkMonitor.Api.Features.Speed.Models;

public static class SpeedTestQualityEvaluator
{
    public static string GetPingQuality(double pingMs) => pingMs switch
    {
        < SpeedTestConstants.QualityThresholds.PingExcellent => "Excellent",
        < SpeedTestConstants.QualityThresholds.PingGood       => "Good",
        < SpeedTestConstants.QualityThresholds.PingFair       => "Fair",
        _                                                    => "Poor"
    };

    public static string GetDownloadQuality(double mbps) => mbps switch
    {
        > SpeedTestConstants.QualityThresholds.DownloadExcellent => "Excellent",
        > SpeedTestConstants.QualityThresholds.DownloadGood       => "Good",
        > SpeedTestConstants.QualityThresholds.DownloadFair       => "Fair",
        _                                                        => "Poor"
    };

    public static string GetUploadQuality(double mbps) => mbps switch
    {
        > SpeedTestConstants.QualityThresholds.UploadExcellent => "Excellent",
        > SpeedTestConstants.QualityThresholds.UploadGood       => "Good",
        > SpeedTestConstants.QualityThresholds.UploadFair       => "Fair",
        _                                                      => "Poor"
    };
}