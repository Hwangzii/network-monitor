using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorApp.Models
{
    public sealed class TimelineTick
    {
        public double X { get; set; }
        public string Label { get; set; } = "";
        public double Left { get; set; }
        public double LabelWidth { get; set; }
    }
}
