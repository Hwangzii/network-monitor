using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MonitorApp.Models
{
    public class TimeTick
    {
        public double Left { get; set; }
        public string Label { get; set; } = "";
        public double LabelWidth { get; set; } = 80;

        // ✅ Alias để XAML bind Width="{Binding ItemWidth}" chạy đúng
        public double ItemWidth => LabelWidth;
    }
}


