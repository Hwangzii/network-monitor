using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace MonitorApp.Views.Pages.TrafficMonitor.TM_Center
{
    /// <summary>
    /// Interaction logic for Usage.xaml
    /// </summary>
    public partial class Usage : UserControl
    {
        public Usage()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                Debug.WriteLine("🧩 Usage View Loaded");
                Debug.WriteLine($"📌 DataContext = {DataContext?.GetType().Name}");
            };
        }
    }
}
