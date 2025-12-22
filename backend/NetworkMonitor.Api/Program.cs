// file: NetworkMonitor.Api/Program.cs
using System.IO;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Api.Features.Speed.Data;
using NetworkMonitor.Api.Features.Speed.Services;

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

// === SQLITE HISTORY - Đặt trong thư mục project gốc/Data ===
var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataFolder); // Tự tạo folder Data nếu chưa có

var dbPath = Path.Combine(dataFolder, "speedtest_history.db");

// Đăng ký DbContextFactory
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

// TỰ ĐỘNG TẠO DATABASE + SCHEMA NẾU CHƯA TỒN TẠI
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpeedTestDbContext>>();
    using var context = dbFactory.CreateDbContext();
    context.Database.EnsureCreated(); // Tạo file db + bảng nếu chưa có
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();