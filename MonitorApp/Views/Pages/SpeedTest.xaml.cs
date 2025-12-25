using System;
using System.Collections.Generic;
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

namespace MonitorApp.Views.Pages
{
    public partial class SpeedTest : UserControl
    {
        public SpeedTest()
        {
            InitializeComponent();
        }

        // ✅ Fix CS1061: XAML đang gọi SelectionChanged="DataGrid_SelectionChanged"
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: xử lý nếu cần, còn không để trống cũng được
        }
    }
}

