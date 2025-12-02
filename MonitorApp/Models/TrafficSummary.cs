using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorApp.Models
{
    public class TrafficSummary
    {
        public string TotalUsage { get; set; }
        public string DownloadTotal { get; set; }
        public string DownloadSpeed { get; set; }
        public string UploadTotal { get; set; }
        public string UploadSpeed { get; set; }
        public string WanUsage { get; set; }
        public string LanUsage { get; set; }
        public double DownloadRatio { get; set; }
        public double UploadRatio { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

