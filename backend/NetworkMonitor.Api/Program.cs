using System.Net; // Để dùng IPAddress nếu cần sau này

var builder = WebApplication.CreateBuilder(args);

// Cấu hình URLs cho Render (listen tất cả IP, port dynamic)
builder.WebHost.UseUrls("http://0.0.0.0:${PORT}");

// Add services to the container.
builder.Services.AddControllers(); // Đăng ký MonitorController

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Enable Swagger ở prod cho demo (test trên Render)
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetworkMonitor API v1"));
}

// Thêm routing để xử lý routes từ controllers
app.UseRouting();

// Comment HTTPS redirection vì Render handle tự động
// app.UseHttpsRedirection();

// Map controllers để kích hoạt routes như /monitor/*
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