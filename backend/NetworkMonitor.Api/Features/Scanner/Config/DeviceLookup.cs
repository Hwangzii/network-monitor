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
        { "Smart TV", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9ImN1cnJlbnRDb2xvciIgc3Ryb2tlLXdpZHRoPSIyIiBzdHJva2UtbGluZWNhcD0icm91bmQiIHN0cm9rZS1saW5lam9pbj0icm91bmQiIGNsYXNzPSJsdWNpZGUgbHVjaWRlLXR2LWljb24gbHVjaWRlLXR2Ij48cGF0aCBkPSJtMTcgMi01IDUtNS01Ii8+PHJlY3Qgd2lkdGg9IjIwIiBoZWlnaHQ9IjE1IiB4PSIyIiB5PSI3IiByeD0iMiIvPjwvc3ZnPg==" },
        { "Generic", "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9ImN1cnJlbnRDb2xvciIgc3Ryb2tlLXdpZHRoPSIyIiBzdHJva2UtbGluZWNhcD0icm91bmQiIHN0cm9rZS1saW5lam9pbj0icm91bmQiIGNsYXNzPSJsdWNpZGUgbHVjaWRlLWhhdC1nbGFzc2VzLWljb24gbHVjaWRlLWhhdC1nbGFzc2VzIj48cGF0aCBkPSJNMTQgMThhMiAyIDAgMCAwLTQgMCIvPjxwYXRoIGQ9Im0xOSAxMS0yLjExLTYuNjU3YTIgMiAwIDAgMC0yLjc1Mi0xLjE0OGwtMS4yNzYuNjFBMiAyIDAgMCAxIDEyIDRIOC41YTIgMiAwIDAgMC0xLjkyNSAxLjQ1Nkw1IDExIi8+PHBhdGggZD0iTTIgMTFoMjAiLz48Y2lyY2xlIGN4PSIxNyIgY3k9IjE4IiByPSIzIi8+PGNpcmNsZSBjeD0iNyIgY3k9IjE4IiByPSIzIi8+PC9zdmc+" }
    };

    public static string GetIconUrl(string type) 
        => TypeIcons.TryGetValue(type, out var url) ? url : TypeIcons["Generic"];

    // 2. Tra cứu Vendor và Type sơ bộ từ OUI
    public static (string Vendor, string Type) GetVendorInfo(string mac)
{
    if (string.IsNullOrWhiteSpace(mac) || mac.Length < 8) return ("Unknown", "Generic");
    
    // Lấy 6 ký tự đầu (OUI) và chuẩn hóa
    var oui = mac[..8].Replace(":", "").Replace("-", "").ToUpperInvariant();

    var map = new Dictionary<string, (string Vendor, string Type)>(StringComparer.OrdinalIgnoreCase)
    {
        // === DỮ LIỆU TỪ ẢNH CUNG CẤP ===
        { "127928", ("Samsung", "Mobile Phone") }, // S10-cua-Thanh-Hang
        { "E0D362", ("TP-Link", "Generic") },      // Generic (TP-Link)
        { "E02BE9", ("Lenovo", "Laptop") },       // hazii-ThinkPad-L15-Gen-2
        { "AE84C6", ("Generic", "Mobile Phone") }, // Android-2 / Generic
        { "6CEBB6", ("Huawei", "Router") },       // Router (Huawei)
        { "F49634", ("Intel", "Desktop") },       // DESKTOP-U5P4O52
        { "82E326", ("Apple", "Mobile Phone") },   // Mobile Phone (Apple - iOS)
        { "C40D96", ("Huawei", "Wi-Fi") },        // Wi-Fi (Huawei EchoLife)
        { "2A15CA", ("Oppo", "Mobile Phone") },   // OPPO-A5-2020
        { "3A3649", ("Generic", "Generic") },      // Thiết bị Generic đầu danh sách
        { "16D4C4", ("Generic", "Generic") },      // Thiết bị Generic 192.168.1.208

        // === DỮ LIỆU BỔ SUNG PHỔ BIẾN ===
        { "B0B867", ("TP-Link", "Router") }, 
        { "C8D3A3", ("TP-Link", "Router") }, 
        { "F81A67", ("TP-Link", "Router") }, 
        { "3C5A37", ("Samsung", "Mobile Phone") }, 
        { "5C3A35", ("Samsung", "Mobile Phone") }, 
        { "7C3A37", ("Samsung", "Mobile Phone") }, 
        { "ACD1A3", ("Xiaomi", "Router") },
        { "ACD1B8", ("Xiaomi", "Router") }, 
        { "D4F4BE", ("Apple", "Mobile Phone") }, 
        { "F4F5D8", ("Apple", "Mobile Phone") }, 
        { "04E536", ("Apple", "Mobile Phone") },
        { "00D49E", ("Dell", "Laptop") },
        { "D8C359", ("ASUS", "Laptop") }, 
        { "E0B9BA", ("Samsung", "Smart TV") }
    };

    return map.TryGetValue(oui, out var v) ? v : ("Unknown", "Generic");
}
}