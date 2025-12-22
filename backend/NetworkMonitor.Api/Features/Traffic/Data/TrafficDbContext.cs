using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Api.Features.Traffic.Models;

namespace NetworkMonitor.Api.Features.Traffic.Data;

public class TrafficDbContext : DbContext
{
    public TrafficDbContext(DbContextOptions<TrafficDbContext> options)
        : base(options) { }

    public DbSet<TrafficSample> TrafficSamples => Set<TrafficSample>();
}
