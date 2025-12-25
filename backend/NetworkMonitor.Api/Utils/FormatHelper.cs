// NetworkMonitor.Api/Utils/FormatHelper.cs
namespace NetworkMonitor.Api.Utils;

public static class FormatHelper
{
    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        if (bytes == 0) return "0 B";

        int index = 0;
        double size = bytes;

        while (size >= 1024 && index < suffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return $"{size:0.##} {suffixes[index]}";
    }

    public static string FormatSpeed(double bytesPerSecond)
    {
        return $"{FormatBytes((long)bytesPerSecond)}/s";
    }
}