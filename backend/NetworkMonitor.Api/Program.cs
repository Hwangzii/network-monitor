using System.Net; // Để dùng IPAddress nếu cần sau này
using System; // Cho Environment

var builder = WebApplication.CreateBuilder(args);

// Cấu hình URLs: Localhost cho dev (tránh permission issue), 0.0.0.0 cho Render
var isDevelopment = builder.Environment.IsDevelopment();
var port = Environment.GetEnvironmentVariable("PORT") ?? "5002";
var urls = isDevelopment 
    ? $"http://localhost:{port}"  // Chỉ localhost cho local (an toàn trên Linux)
    : $"http://0.0.0.0:{port}";  // All interfaces cho cloud
builder.WebHost.UseUrls(urls);

// Add services to the container.
builder.Services.AddControllers(); // Đăng ký MonitorController

// Swagger/OpenAPI - Fix để generate /swagger.json đúng
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "NetworkMonitor API", Version = "v1" }); // Explicit doc cho v1
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger(); // Map /swagger.json
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetworkMonitor API v1"); 
    c.RoutePrefix = string.Empty; // UI ở root / (dễ test)
});

if (app.Environment.IsDevelopment())
{
    // Dev extras nếu cần
}
else
{
    // Prod: Swagger enable cho demo
}

app.UseRouting();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}