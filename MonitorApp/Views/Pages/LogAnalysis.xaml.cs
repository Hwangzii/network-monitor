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
    /// <summary>
    /// Interaction logic for LogAnalysis.xaml
    /// </summary>
    public partial class LogAnalysis : UserControl
    {
        public LogAnalysis()
        {
            InitializeComponent();
        }

        // Các hàm này hiện chưa được gắn vào nút nào bên XAML
        // Bạn có thể viết logic xử lý tại đây sau này
        private void OpenExportModal_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}