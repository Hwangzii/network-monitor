using System.Windows;
using System.Windows.Controls;
using MonitorApp.ViewModels.PageViewModels.TrafficMonitor;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MonitorApp.Views.Pages.TrafficMonitor.TM_Center
{
    public partial class Graph : UserControl
    {
        private GraphViewModel? _viewModel;
        private bool _dragging;

        private readonly ToolTip _hoverTip = new ToolTip
        {
            Placement = PlacementMode.Relative,
            StaysOpen = true
        };

        private void ChartCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;

            double xAbs = e.GetPosition(ChartCanvas).X + ChartScroller.HorizontalOffset;

            // tooltip
            if (_viewModel.TryGetHoverInfo(xAbs, out var text, out var snapXAbs))
            {
                _hoverTip.Content = text;
                _hoverTip.HorizontalOffset = (snapXAbs - ChartScroller.HorizontalOffset) + 12;
                _hoverTip.VerticalOffset = e.GetPosition(ChartCanvas).Y + 12;
                _hoverTip.IsOpen = true;
            }
            else
            {
                _hoverTip.IsOpen = false;
            }

            // crosshair
            _viewModel.UpdateHover(xAbs, ChartCanvas.ActualHeight, ChartScroller.HorizontalOffset);
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
                ChartCanvas.MouseLeave += (_, __2) => _viewModel?.ClearHover();
                ChartCanvas.MouseLeftButtonDown += ChartCanvas_MouseLeftButtonDown;
                ChartCanvas.MouseLeftButtonUp += ChartCanvas_MouseLeftButtonUp;
                ChartCanvas.MouseMove += ChartCanvas_MouseMove_Select;


            };

            SizeChanged += (_, __) =>
            {
                (_viewModel ??= DataContext as GraphViewModel)
                    ?.SetViewportWidth(ActualWidth);
            };
        }
        private void ChartCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null) return;

            _dragging = true;
            ChartCanvas.CaptureMouse();

            double xAbs = e.GetPosition(ChartCanvas).X + ChartScroller.HorizontalOffset;
            _viewModel.BeginSelection(xAbs);

            e.Handled = true;
        }

        private void ChartCanvas_MouseMove_Select(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;
            if (!_dragging) return;
            if (Mouse.LeftButton != MouseButtonState.Pressed) return;

            double xAbs = e.GetPosition(ChartCanvas).X + ChartScroller.HorizontalOffset;
            _viewModel.UpdateSelection(xAbs);
        }

        private void ChartCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null) return;
            if (!_dragging) return;

            double xAbs = e.GetPosition(ChartCanvas).X + ChartScroller.HorizontalOffset;
            _viewModel.EndSelection(xAbs);

            _dragging = false;
            ChartCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        public void ChangeRange(string range)
        {
            (_viewModel ??= DataContext as GraphViewModel)?.ChangeRange(range);
        }

        private void MaxDropBtn_Click(object sender, RoutedEventArgs e)
        {
            // Toggle popup chắc chắn
            if (ScalePopup != null)
                ScalePopup.IsOpen = !ScalePopup.IsOpen;
        }

        private void CloseScalePopup_Click(object sender, RoutedEventArgs e)
        {
            if (ScalePopup != null)
                ScalePopup.IsOpen = false;
        }
        private void AutoScale_CheckedChanged_ClosePopup(object sender, RoutedEventArgs e)
        {
            if (ScalePopup != null) ScalePopup.IsOpen = false;
        }


    }
}
