// File: NetworkMonitor.Api/Features/Scanner/Config/DeviceLookup.cs
namespace NetworkMonitor.Api.Features.Scanner.Config;

public static class DeviceLookup
{
    // 1. Bản đồ Icon dựa trên phân loại thiết bị (Type)
    public static readonly Dictionary<string, string> TypeIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Router", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1yb3V0ZXItaWNvbiBsdWNpZGUtcm91dGVyIj48cmVjdCB3aWR0aD0iMjAiIGhlaWdodD0iOCIgeD0iMiIgeT0iMTQiIHJ4PSIyIi8+PHBhdGggZD0iTTYuMDEgMThINiIvPjxwYXRoIGQ9Ik0xMC4wMSAxOEgxMCIvPjxwYXRoIGQ9Ik0xNSAxMHY0Ii8+PHBhdGggZD0iTTE3Ljg0IDcuMTdhNCA0IDAgMCAwLTUuNjYgMCIvPjxwYXRoIGQ9Ik0yMC42NiA0LjM0YTggOCAwIDAgMC0xMS4zMSAwIi8+PC9zdmc+" },
        { "Mobile Phone", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1zbWFydHBob25lLWljb24gbHVjaWRlLXNtYXJ0cGhvbmUiPjxyZWN0IHdpZHRoPSIxNCIgaGVpZ2h0PSIyMCIgeD0iNSIgeT0iMiIgcng9IjIiIHJ5PSIyIi8+PHBhdGggZD0iTTEyIDE4aC4wMSIvPjwvc3ZnPg==" },
        { "Laptop", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1sYXB0b3AtaWNvbiBsdWNpZGUtbGFwdG9wIj48cGF0aCBkPSJNMTggNWEyIDIgMCAwIDEgMiAydjguNTI2YTIgMiAwIDAgMCAuMjEyLjg5N2wxLjA2OCAyLjEyN2ExIDEgMCAwIDEtLjkgMS40NUgzLjYyYTEgMSAwIDAgMS0uOS0xLjQ1bDEuMDY4LTIuMTI3QTIgMiAwIDAgMCA0IDE1LjUyNlY3YTIgMiAwIDAgMSAyLTJ6Ii8+PHBhdGggZD0iTTIwLjA1NCAxNS45ODdIMy45NDYiLz48L3N2Zz4=" },
        { "Desktop", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1tb25pdG9yLWljb24gbHVjaWRlLW1vbml0b3IiPjxyZWN0IHdpZHRoPSIyMCIgaGVpZ2h0PSIxNCIgeD0iMiIgeT0iMyIgcng9IjIiLz48bGluZSB4MT0iOCIgeDI9IjE2IiB5MT0iMjEiIHkyPSIyMSIvPjxsaW5lIHgxPSIxMiIgeDI9IjEyIiB5MT0iMTciIHkyPSIyMSIvPjwvc3ZnPg==" },
        { "Wi-Fi", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS13aWZpLWljb24gbHVjaWRlLXdpZmkiPjxwYXRoIGQ9Ik0xMiAyMGguMDEiLz48cGF0aCBkPSJNMiA4LjgyYTE1IDE1IDAgMCAxIDIwIDAiLz48cGF0aCBkPSJNNSAxMi44NTlhMTAgMTAgMCAwIDEgMTQgMCIvPjxwYXRoIGQ9Ik04LjUgMTYuNDI5YTUgNSAwIDAgMSA3IDAiLz48L3N2Zz4=" },
        { "Smart TV", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS10di1pY29uIGx1Y2lkZS10diI+PHBhdGggZD0ibTE3IDItNSA1LTUtNSIvPjxyZWN0IHdpZHRoPSIyMCIgaGVpZ2h0PSIxNSIgeD0iMiIgeT0iNyIgcng9IjIiLz48L3N2Zz4=" },
        { "Generic", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9IiNBREFEQUQiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIiBjbGFzcz0ibHVjaWRlIGx1Y2lkZS1naXQtZm9yay1pY29uIGx1Y2lkZS1naXQtZm9yayI+PGNpcmNsZSBjeD0iMTIiIGN5PSIxOCIgcj0iMyIvPjxjaXJjbGUgY3g9IjYiIGN5PSI2IiByPSIzIi8+PGNpcmNsZSBjeD0iMTgiIGN5PSI2IiByPSIzIi8+PHBhdGggZD0iTTE4IDl2MmMwIC42LS40IDEtMSAxSDdjLS42IDAtMS0uNC0xLTFWOSIvPjxwYXRoIGQ9Ik0xMiAxMnYzIi8+PC9zdmc+" }
    };

    public static string GetIconUrl(string type) 
        => TypeIcons.TryGetValue(type, out var url) ? url : TypeIcons["Generic"];

    // 2. Tra cứu Vendor và Type sơ bộ từ OUI
    public static (string Vendor, string Type) GetVendorInfo(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac) || mac.Length < 8) return ("Unknown", "Generic");
        var oui = mac[..8].Replace(":", "").Replace("-", "").ToUpperInvariant();

        var map = new Dictionary<string, (string Vendor, string Type)>(StringComparer.OrdinalIgnoreCase)
        {
            {"B0B867", ("TP-Link", "Router")}, {"C8D3A3", ("TP-Link", "Router")}, {"F81A67", ("TP-Link", "Router")},
            {"6CE8B6", ("Huawei", "Router")}, {"ACD1B8", ("Xiaomi", "Router")}, {"C40D96", ("Huawei", "Wi-Fi")},
            {"D4F4BE", ("Apple", "Mobile Phone")}, {"F4F5D8", ("Apple", "Mobile Phone")}, {"04E536", ("Apple", "Mobile Phone")},
            {"82E326", ("Mobile Device", "Mobile Phone")}, {"2A15CA", ("Oppo", "Mobile Phone")},
            {"E029E9", ("Lenovo", "Laptop")}, {"F49634", ("Intel", "Desktop")}, {"00D49E", ("Dell", "Laptop")},
            {"D8C359", ("ASUS", "Laptop")}, {"E0B9BA", ("Samsung", "Smart TV")}, {"3A3649", ("Generic", "Generic")}
        };

        return map.TryGetValue(oui, out var v) ? v : ("Unknown", "Generic");
    }
}