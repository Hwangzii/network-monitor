using System;
using System.Windows.Input;

namespace MonitorApp.ViewModels.Layouts
{
    public class HeaderViewModel
    {
        public event Action<string>? PageChanged;

        public ICommand ShowTrafficMonitorCommand { get; }
        public ICommand ShowGlassWireProtectCommand { get; }

        public HeaderViewModel()
        {
            ShowTrafficMonitorCommand = new RelayCommand(_ => OnPageChanged("Traffic"));
            ShowGlassWireProtectCommand = new RelayCommand(_ => OnPageChanged("GlassWire"));
        }

        private void OnPageChanged(string pageName)
        {
            PageChanged?.Invoke(pageName);
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
