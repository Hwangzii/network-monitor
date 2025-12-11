using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace NetworkMonitor.Api.Utils;

[SupportedOSPlatform("windows")]
public static class IconExtractor
{
    public static string? GetBase64IconFromExe(string exePath)
    {
        try
        {
            if (!File.Exists(exePath)) return null;

            using Icon? icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon == null) return null;

            using var ms = new MemoryStream();
            icon.ToBitmap().Save(ms, ImageFormat.Png);
            byte[] bytes = ms.ToArray();
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }

    // Dành cho trường hợp chỉ có tên process (ví dụ: msedge.exe) → tìm trong các process đang chạy
    public static string? GetIconFromProcessName(string processName)
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(processName));

            foreach (var p in processes)
            {
                try
                {
                    string? path = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        return GetBase64IconFromExe(path);
                    }
                }
                catch { /* Access denied → bỏ qua */ }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch { }

        // Fallback: thử tìm trong thư mục phổ biến
        string[] commonPaths =
        {
            $@"C:\Program Files\{processName}",
            $@"C:\Program Files (x86)\{processName}",
            $@"C:\Users\{Environment.UserName}\AppData\Local\{processName}",
            $@"C:\Program Files\WindowsApps\"
        };

        foreach (var folder in commonPaths)
        {
            if (Directory.Exists(folder))
            {
                var exe = Directory.GetFiles(folder, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Contains(processName, StringComparison.OrdinalIgnoreCase));
                if (exe != null) return GetBase64IconFromExe(exe);
            }
        }

        return null;
    }
}