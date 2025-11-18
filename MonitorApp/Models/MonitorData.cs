namespace MonitorApp.Models
{
    public class MonitorData
    {
        public string Ip { get; set; }
        public double BytesSent { get; set; }
        public double BytesReceived { get; set; }
        public double BandwidthKBps { get; set; }
        public bool IsMonitoring { get; set; }
    }
}
