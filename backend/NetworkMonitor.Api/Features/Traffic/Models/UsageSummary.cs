// file: NetworkMonitor.Api/Features/Traffic/Models/UsageSummary.cs
using System;

namespace NetworkMonitor.Api.Features.Traffic.Models;

public class UsageSummary
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string JsonData { get; set; } = string.Empty;
}