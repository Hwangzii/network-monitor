// file: NetworkMonitor.Api/Program.cs
using System.IO;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure; // Thêm namespace này
// using QuestPDF.Settings;       // Thêm namespace này

// ===== FEATURES =====
using NetworkMonitor.Api.Features.Speed.Data;
using NetworkMonitor.Api.Features.Speed.Services;
using NetworkMonitor.Api.Features.Traffic.Data;
using NetworkMonitor.Api.Features.Traffic.Services;
using NetworkMonitor.Api.Services.Scanner;
using NetworkMonitor.Api.Services.Firewall;
using NetworkMonitor.Api.Services.NetworkMonitor;
using NetworkMonitor.Api.Services.Traffic;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CẤU HÌNH QUESTPDF LICENSE (QUAN TRỌNG)
// ==========================================
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =====================
// CORE SERVICES
// =====================
builder.Services.AddSingleton<IWifiService, WifiService>();
builder.Services.AddSingleton<INetworkScannerService, NetworkScannerService>();

// =====================
// DATA FOLDER
// =====================
var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "Data");
if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);

// =====================
// SPEEDTEST FEATURE
// =====================
var speedDbPath = Path.Combine(dataFolder, "speedtest_history.db");
builder.Services.AddDbContextFactory<SpeedTestDbContext>(options =>
    options.UseSqlite($"Data Source={speedDbPath}"));
builder.Services.AddScoped<SpeedTestHistoryService>();
builder.Services.AddScoped<SpeedTestService>();

// =====================
// TRAFFIC FEATURE
// =====================
builder.Services.AddSingleton<TrafficUsageService>();
var trafficDbPath = Path.Combine(dataFolder, "traffic.db");
builder.Services.AddDbContext<TrafficDbContext>(options =>
    options.UseSqlite($"Data Source={trafficDbPath}"));

// Đăng ký các service quản lý dữ liệu traffic
builder.Services.AddSingleton<TrafficSampler>();
builder.Services.AddHostedService<TrafficCollector>();

// Đăng ký các service phục vụ API & Report
builder.Services.AddSingleton<TrafficService>();        // realtime summary
builder.Services.AddScoped<TrafficChartService>();      // history chart (đã dọn dẹp trùng lặp)
builder.Services.AddSingleton<TrafficReportService>();  // xuất PDF

// =====================
// FIREWALL (WINDOWS ONLY)
// =====================
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<INetworkStatusChecker, NetworkStatusChecker>();
    builder.Services.AddSingleton<INetworkTrafficMonitor, EtwNetworkTrafficMonitor>();
    builder.Services.AddScoped<IFirewallService, FirewallService>();
}

var app = builder.Build();

// =====================
// AUTO CREATE DATABASES
// =====================
using (var scope = app.Services.CreateScope())
{
    var speedDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpeedTestDbContext>>();
    using var speedDb = speedDbFactory.CreateDbContext();
    speedDb.Database.EnsureCreated();

    var trafficDb = scope.ServiceProvider.GetRequiredService<TrafficDbContext>();
    trafficDb.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();