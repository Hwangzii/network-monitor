// file: NetworkMonitor.Api/Program.cs
using System.IO; // <-- THÊM cho Path và Directory
using Microsoft.EntityFrameworkCore; // <-- CHO SQLITE
using NetworkMonitor.Api.Features.Speed.Data; // <-- CHO SpeedTestDbContext
using NetworkMonitor.Api.Features.Speed.Services; // <-- CHO SpeedTestService và SpeedTestHistoryService

using NetworkMonitor.Api.Services.Traffic;
using NetworkMonitor.Api.Services.Scanner;
using NetworkMonitor.Api.Services.Firewall;
using NetworkMonitor.Api.Services.NetworkMonitor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Các service cũ
builder.Services.AddSingleton<IWifiService, WifiService>();
builder.Services.AddSingleton<TrafficService>();
builder.Services.AddSingleton<INetworkScannerService, NetworkScannerService>();

// === SQLITE HISTORY - THƯ MỤC GỐC /Data ===
var dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");
Directory.CreateDirectory(dataFolder);
var dbPath = Path.Combine(dataFolder, "speedtest_history.db");

// ĐĂNG KÝ DbContextFactory (bắt buộc cho IDbContextFactory injection)
builder.Services.AddDbContextFactory<SpeedTestDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<SpeedTestHistoryService>();
builder.Services.AddScoped<SpeedTestService>(); // Scoped để inject HistoryService

// Firewall services
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<INetworkStatusChecker, NetworkStatusChecker>();
    builder.Services.AddSingleton<INetworkTrafficMonitor, EtwNetworkTrafficMonitor>();
    builder.Services.AddScoped<IFirewallService, FirewallService>();
}
else
{
    Console.WriteLine("Warning: Firewall services are only available on Windows platform.");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();