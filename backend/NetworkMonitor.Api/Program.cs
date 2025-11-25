// file: NetworkMonitor.Api/Program.cs

// using NetworkMonitor.Api.Services.System;
using NetworkMonitor.Api.Services.Traffic;
using NetworkMonitor.Api.Services.Scanner;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// builder.Services.AddSingleton<LocalSystemService>();
builder.Services.AddSingleton<TrafficService>();
builder.Services.AddSingleton<INetworkScannerService, NetworkScannerService>();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
