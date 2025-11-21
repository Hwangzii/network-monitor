// ViewModels/PageViewModels/TrafficMonitor/TrafficMonitorViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonitorApp.ViewModels.PageViewModels.TrafficMonitor
{
    public class TrafficMonitorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _selectedRange = "5m"; // mặc định 5 phút
        public string SelectedRange
        {
            get => _selectedRange;
            set
            {
                if (_selectedRange != value)
                {
                    _selectedRange = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedRangeDisplay)); // thêm dòng này để ComboBox cập nhật text
                    FooterViewModel.RefreshCommand.Execute(null);
                }
            }
        }

        // Thuộc tính hiển thị 5m, 15m, 24h...
        public string SelectedRangeDisplay => SelectedRange switch
        {
            "5m" => "5m",
            "15m" => "15m",
            "30m" => "30m",
            "1h" => "1h",
            "24h" => "24h",
            _ => "5m"
        };

        public TM_HeaderViewModel HeaderViewModel { get; } = new();
        public TM_CenterMainViewModel CenterMainViewModel { get; } = new();
        public TM_FooterViewModel FooterViewModel { get; } = new();

        public TrafficMonitorViewModel()
        {
            HeaderViewModel.ParentViewModel = this;
            FooterViewModel.ParentViewModel = this;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}