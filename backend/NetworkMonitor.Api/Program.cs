// file: NetworkMonitor.Api/Program.cs

using NetworkMonitor.Api.Services.Traffic;
using NetworkMonitor.Api.Services.Scanner;
using NetworkMonitor.Api.Services.Firewall;
using NetworkMonitor.Api.Services.NetworkMonitor; // <-- THÊM


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IWifiService, WifiService>();
builder.Services.AddSingleton<TrafficService>();
builder.Services.AddSingleton<INetworkScannerService, NetworkScannerService>();

// Đăng ký Firewall Services (Chỉ trên Windows)
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<INetworkStatusChecker, NetworkStatusChecker>();

    // 🔥 BẮT BUỘC – ETW Traffic Monitor
    builder.Services.AddSingleton<INetworkTrafficMonitor, EtwNetworkTrafficMonitor>();

    // Firewall service
    builder.Services.AddScoped<IFirewallService, FirewallService>();
}
else
{
    Console.WriteLine("Warning: Firewall services are only available on Windows platform.");
}


var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.MapControllers();
app.Run();