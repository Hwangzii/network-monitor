// Features/Speed/Services/SpeedTestHistoryService.cs
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Api.Features.Speed.Data;
using NetworkMonitor.Api.Features.Speed.Models;

namespace NetworkMonitor.Api.Features.Speed.Services;

public class SpeedTestHistoryService
{
    private readonly IDbContextFactory<SpeedTestDbContext> _dbFactory;

    // Giới hạn số bản ghi lưu trữ (có thể config sau nếu cần)
    private const int MaxHistoryItems = 100;

    public SpeedTestHistoryService(IDbContextFactory<SpeedTestDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Thêm một kết quả speed test mới vào lịch sử
    /// </summary>
    public async Task AddResultAsync(double downloadMbps, double uploadMbps, double pingMs)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var item = new SpeedTestHistoryItem
        {
            Timestamp = DateTime.UtcNow,
            DownloadMbps = downloadMbps,
            UploadMbps = uploadMbps,
            PingMs = pingMs
        };

        context.SpeedTestHistory.Add(item);
        await context.SaveChangesAsync();

        // === TỰ ĐỘNG DỌN DẸP: Giữ lại tối đa MaxHistoryItems bản ghi mới nhất ===
        var totalCount = await context.SpeedTestHistory.CountAsync();
        if (totalCount > MaxHistoryItems)
        {
            var itemsToRemove = await context.SpeedTestHistory
                .OrderByDescending(h => h.Timestamp)
                .Skip(MaxHistoryItems)
                .Take(totalCount - MaxHistoryItems)
                .ToListAsync();

            if (itemsToRemove.Any())
            {
                context.SpeedTestHistory.RemoveRange(itemsToRemove);
                await context.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Lấy danh sách lịch sử (mới nhất trước)
    /// </summary>
    public async Task<List<SpeedTestHistoryItem>> GetHistoryAsync(int limit = 20)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        if (limit < 1) limit = 20;
        if (limit > MaxHistoryItems) limit = MaxHistoryItems;

        return await context.SpeedTestHistory
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tổng số bản ghi hiện có trong lịch sử
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        return await context.SpeedTestHistory.CountAsync();
    }
}