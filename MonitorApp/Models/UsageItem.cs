using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// File: Models/UsageItems.cs
using System.Windows.Media;

namespace MonitorApp.Models
{
    public class UsageAppItem
    {
        public ImageSource? Icon { get; set; }
        public ImageSource? Flag { get; set; }
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public double Progress { get; set; }   // 0..100
    }

    public class UsageHostItem
    {
        public ImageSource? Icon { get; set; }
        public ImageSource? Flag { get; set; }
        public string Host { get; set; } = "";
        public string Size { get; set; } = "";
        public double Progress { get; set; }   // 0..100
    }

    public class UsageTrafficTypeItem
    {
        public string Type { get; set; } = "";
        public string Size { get; set; } = "";
        public double Progress { get; set; }   // 0..100
    }

    public class UsageCountryItem
    {
        public ImageSource? Flag { get; set; }
        public string Country { get; set; } = "";
        public string Size { get; set; } = "";
        public double Progress { get; set; }   // 0..100
    }
}


