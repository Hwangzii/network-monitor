using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// NetworkMonitor.Core/Models/MonitorStatus.cs
namespace NetworkMonitor.Core.Models
{
    public class MonitorStatus
    {
        public string Ip { get; set; } = "";
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public double BandwidthKBps { get; set; }
        public bool IsMonitoring { get; set; }
    }
}

