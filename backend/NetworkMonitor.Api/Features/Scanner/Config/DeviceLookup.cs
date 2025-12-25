using System.Text.Json;

namespace NetworkMonitor.Api.Features.Scanner.Config;

public static class DeviceLookup
{
    private static Dictionary<string, DeviceVendorInfo> _vendorMap = new(StringComparer.OrdinalIgnoreCase);

    // Cấu trúc dữ liệu trong JSON
    public class DeviceVendorInfo 
    {
        public string Vendor { get; set; } = "";
        public string Type { get; set; } = "";
    }

    // Bản đồ Icon giữ nguyên trong code vì là dữ liệu tĩnh của UI
    public static readonly Dictionary<string, string> TypeIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Router", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1yb3V0ZXItaWNvbiBsdWNpZGUtcm91dGVyIj48cmVjdCB3aWR0aD0iMjAiIGhlaWdodD0iOCIgeD0iMiIgeT0iMTQiIHJ4PSIyIi8+PHBhdGggZD0iTTYuMDEgMThINiIvPjxwYXRoIGQ9Ik0xMC4wMSAxOEgxMCIvPjxwYXRoIGQ9Ik0xNSAxMHY0Ii8+PHBhdGggZD0iTTE3Ljg0IDcuMTdhNCA0IDAgMCAwLTUuNjYgMCIvPjxwYXRoIGQ9Ik0yMC42NiA0LjM0YTggOCAwIDAgMC0xMS4zMSAwIi8+PC9zdmc+" },
        { "Mobile Phone", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1zbWFydHBob25lLWljb24gbHVjaWRlLXNtYXJ0cGhvbmUiPjxyZWN0IHdpZHRoPSIxNCIgaGVpZ2h0PSIyMCIgeD0iNSIgeT0iMiIgcng9IjIiIHJ5PSIyIi8+PHBhdGggZD0iTTEyIDE4aC4wMSIvPjwvc3ZnPg==" },
        { "Laptop", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1sYXB0b3AtaWNvbiBsdWNpZGUtbGFwdG9wIj48cGF0aCBkPSJNMTggNWEyIDIgMCAwIDEgMiAydjguNTI2YTIgMiAwIDAgMCAuMjEyLjg5N2wxLjA2OCAyLjEyN2ExIDEgMCAwIDEtLjkgMS40NUgzLjYyYTEgMSAwIDAgMS0uOS0xLjQ1bDEuMDY4LTIuMTI3QTIgMiAwIDAgMCA0IDE1LjUyNlY3YTIgMiAwIDAgMSAyLTJ6Ii8+PHBhdGggZD0iTTIwLjA1NCAxNS45ODdIMy45NDYiLz48L3N2Zz4=" },
        { "Desktop", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1tb25pdG9yLWljb24gbHVjaWRlLW1vbml0b3IiPjxyZWN0IHdpZHRoPSIyMCIgaGVpZ2h0PSIxNCIgeD0iMiIgeT0iMyIgcng9IjIiLz48bGluZSB4MT0iOCIgeDI9IjE2IiB5MT0iMjEiIHkyPSIyMSIvPjxsaW5lIHgxPSIxMiIgeDI9IjEyIiB5MT0iMTciIHkyPSIyMSIvPjwvc3ZnPg==" },
        { "Wi-Fi", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS13aWZpLWljb24gbHVjaWRlLXdpZmkiPjxwYXRoIGQ9Ik0xMiAyMGguMDEiLz48cGF0aCBkPSJNMiA4LjgyYTE1IDE1IDAgMCAxIDIwIDAiLz48cGF0aCBkPSJNNSAxMi44NTlhMTAgMTAgMCAwIDEgMTQgMCIvPjxwYXRoIGQ9Ik04LjUgMTYuNDI5YTUgNSAwIDAgMSA3IDAiLz48L3N2Zz4=" },
        { "Smart TV", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9ImN1cnJlbnRDb2xvciIgc3Ryb2tlLXdpZHRoPSIyIiBzdHJva2UtbGluZWNhcD0icm91bmQiIHN0cm9rZS1saW5lam9pbj0icm91bmQiIGNsYXNzPSJsdWNpZGUgbHVjaWRlLXR2LWljb24gbHVjaWRlLXR2Ij48cGF0aCBkPSJtMTcgMi01IDUtNS01Ii8+PHJlY3Qgd2lkdGg9IjIwIiBoZWlnaHQ9IjE1IiB4PSIyIiB5PSI3IiByeD0iMiIvPjwvc3ZnPg==" },
        { "Generic", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1ib3gtaWNvbiBsdWNpZGUtYm94Ij48cGF0aCBkPSJNMjEgOGEyIDIgMCAwIDAtMS0xLjczbC03LTRhMiAyIDAgMCAwLTIgMGwtNyA0QTIgMiAwIDAgMCAzIDh2OGEyIDIgMCAwIDAgMSAxLjczbDcgNGEyIDIgMCAwIDAgMiAwbDctNEEyIDIgMCAwIDAgMjEgMTZaIi8+PHBhdGggZD0ibTMuMyA3IDguNyA1IDguNy01Ii8+PHBhdGggZD0iTTEyIDIyVjEyIi8+PC9zdmc+" }
    };

    static DeviceLookup()
    {
        LoadVendorData();
    }

    private static void LoadVendorData()
    {
        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "device_vendors.json");
            // Nếu không tìm thấy trong bin, thử tìm ở project root (cho lúc debug)
            if (!File.Exists(filePath)) 
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "device_vendors.json");

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                _vendorMap = JsonSerializer.Deserialize<Dictionary<string, DeviceVendorInfo>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new Dictionary<string, DeviceVendorInfo>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading device_vendors.json: {ex.Message}");
        }
    }

    public static string GetIconUrl(string type) 
        => TypeIcons.TryGetValue(type, out var url) ? url : TypeIcons["Generic"];

    public static (string Vendor, string Type) GetVendorInfo(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac) || mac.Length < 8) return ("Unknown", "Generic");
        
        var oui = mac[..8].Replace(":", "").Replace("-", "").ToUpperInvariant();

        if (_vendorMap.TryGetValue(oui, out var info))
        {
            return (info.Vendor, info.Type);
        }

        return ("Unknown", "Generic");
    }
}