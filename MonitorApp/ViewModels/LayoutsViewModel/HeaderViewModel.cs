using System;
using System.Windows.Input;

namespace MonitorApp.ViewModels.Layouts
{
    public class HeaderViewModel
    {
        // Event instance (nếu sau này muốn dùng MVVM thuần)
        public event Action<string>? PageChanged;

        // Event static để MainContent đăng ký dễ dàng
        public static event Action<string>? GlobalPageChanged;

        public ICommand ShowTrafficMonitorCommand { get; }
        public ICommand ShowGlassWireProtectCommand { get; }
        public ICommand ShowLogAnalysisCommand { get; }
        public ICommand ShowNetworkScannerCommand { get; }

        public HeaderViewModel()
        {
            ShowTrafficMonitorCommand = new RelayCommand(_ => OnPageChanged("Traffic"));
            ShowGlassWireProtectCommand = new RelayCommand(_ => OnPageChanged("Protect"));
            ShowLogAnalysisCommand = new RelayCommand(_ => OnPageChanged("Log"));
            ShowNetworkScannerCommand = new RelayCommand(_ => OnPageChanged("Scanner"));
        }

        private void OnPageChanged(string pageName)
        {
            PageChanged?.Invoke(pageName);
            GlobalPageChanged?.Invoke(pageName);   // MainContent sẽ nghe event này
        }
    }

    // Lệnh chung đơn giản để binding trong XAML
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged;
    }
}
