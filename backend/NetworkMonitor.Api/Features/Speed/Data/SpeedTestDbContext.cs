// Features/Speed/Data/SpeedTestDbContext.cs
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Api.Features.Speed.DTOs;
using NetworkMonitor.Api.Features.Speed.Models;

namespace NetworkMonitor.Api.Features.Speed.Data;

public class SpeedTestDbContext : DbContext
{
    public DbSet<SpeedTestHistoryItem> SpeedTestHistory { get; set; } = null!;

    public SpeedTestDbContext(DbContextOptions<SpeedTestDbContext> options) : base(options)
    {
        Database.EnsureCreated(); // Tự động tạo DB + table lần đầu
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpeedTestHistoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.DownloadMbps).IsRequired();
            entity.Property(e => e.UploadMbps).IsRequired();
            entity.Property(e => e.PingMs).IsRequired();

            entity.HasIndex(e => e.Timestamp).IsDescending();
        });
    }
}