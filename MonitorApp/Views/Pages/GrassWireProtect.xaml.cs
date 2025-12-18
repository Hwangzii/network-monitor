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
using MonitorApp.ViewModels.PageViewModels;

namespace MonitorApp.Views.Pages
{
    /// <summary>
    /// Interaction logic for GrassWireProtect.xaml
    /// </summary>
    public partial class GrassWireProtect : UserControl
    {
        public GrassWireProtect()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Detect khi cuộn xuống gần dưới cùng để load thêm dữ liệu
        /// </summary>
        private async void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange <= 0) return;

            var dg = (DataGrid)sender;
            var sv = GetScrollViewer(dg);
            if (sv == null) return;

            if (sv.ScrollableHeight <= 0) return;

            var nearBottom = sv.VerticalOffset >= sv.ScrollableHeight - 20;
            if (nearBottom && DataContext is GrassWireProtectViewModel vm)
                await vm.LoadMoreAsync();
        }


        /// <summary>
        /// Get ScrollViewer từ DataGrid
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t) return t;

                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private ScrollViewer GetScrollViewer(DataGrid dataGrid)
            => FindVisualChild<ScrollViewer>(dataGrid);

    }
}
