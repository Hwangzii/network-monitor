using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorApp.Models
{
    public class TimeTick
    {
        // Canvas.Left của item (đã trừ nửa label width và clamp)
        public double Left { get; set; }

        public string Label { get; set; } = "";

        // Width của item để căn giữa vạch mốc
        public double LabelWidth { get; set; } = 80;
    }
}

