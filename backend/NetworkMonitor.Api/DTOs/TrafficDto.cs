namespace NetworkMonitor.Api.DTOs;
public class TrafficDto { public WanLan Wan { get; set; } = new(); public WanLan Lan { get; set; } = new(); }
public class WanLan { public long Upload { get; set; } public long Download { get; set; } public double SpeedUp { get; set; } public double SpeedDown { get; set; } }
