using System.Windows;
using System.Windows.Controls;
using MonitorApp.ViewModels.PageViewModels.TrafficMonitor;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Input;

namespace MonitorApp.Views.Pages.TrafficMonitor.TM_Center
{
    public partial class Graph : UserControl
    {
        private GraphViewModel? _viewModel;

        private readonly ToolTip _hoverTip = new ToolTip
        {
            Placement = PlacementMode.Relative,
            StaysOpen = true
        };

        private void ChartCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;

            // ChartCanvas nằm trong ScrollViewer => cộng offset để khớp dữ liệu theo ChartWidth
            double x = e.GetPosition(ChartCanvas).X + ChartScroller.HorizontalOffset;

            if (_viewModel.TryGetHoverInfo(x, out var text, out var snapX))
            {
                _hoverTip.Content = text;

                // đặt tooltip theo vị trí snap (trừ offset để hiển thị đúng trên vùng nhìn thấy)
                _hoverTip.HorizontalOffset = (snapX - ChartScroller.HorizontalOffset) + 12;
                _hoverTip.VerticalOffset = e.GetPosition(ChartCanvas).Y + 12;

                _hoverTip.IsOpen = true;
            }
            else
            {
                _hoverTip.IsOpen = false;
            }

            if (_viewModel == null) return;

            double xAbs = e.GetPosition(ChartCanvas).X + ChartScroller.HorizontalOffset;
            double h = ChartCanvas.ActualHeight;

            _viewModel.UpdateHover(xAbs, h, ChartScroller.HorizontalOffset);
        }

        public Graph()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                _viewModel ??= DataContext as GraphViewModel;

                // set viewport width lần đầu
                _viewModel?.SetViewportWidth(ActualWidth);

                // gắn tooltip + mouse events
                _hoverTip.PlacementTarget = ChartCanvas;
                ChartCanvas.ToolTip = _hoverTip;

                ChartCanvas.MouseMove += ChartCanvas_MouseMove;
                ChartCanvas.MouseLeave += (_, __2) => _hoverTip.IsOpen = false;
                ChartCanvas.MouseMove += ChartCanvas_MouseMove;
                ChartCanvas.MouseLeave += (_, __2) => _viewModel?.ClearHover();


            };

            SizeChanged += (_, __) =>
            {
                (_viewModel ??= DataContext as GraphViewModel)
                    ?.SetViewportWidth(ActualWidth);
            };
        }

        public void ChangeRange(string range)
        {
            (_viewModel ??= DataContext as GraphViewModel)?.ChangeRange(range);
        }
    }
}
