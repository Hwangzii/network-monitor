using NetworkMonitor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to container
builder.Services.AddControllers();  // BẮT BUỘC: Đăng ký controllers
builder.Services.AddEndpointsApiExplorer();  // Cho Swagger nếu dùng
builder.Services.AddSwaggerGen();  // Cho Swagger docs
builder.Services.AddSingleton<SystemInfoProvider>();  // DI cho Provider

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();  // Thêm để test Swagger UI tại /swagger
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection();  // COMMENT TẠM: Để tránh warning HTTPS redirect (dùng HTTP cho dev)
app.UseAuthorization();
app.MapControllers();  // BẮT BUỘC: Map tất cả controllers

app.Run();