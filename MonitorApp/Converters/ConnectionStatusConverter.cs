using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MonitorApp.Converters
{
    /// <summary>
    /// Convert connection status (Allowed/Blocked) to background color
    /// </summary>
    public class ConnectionStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() == "allowed" 
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")) // Light green
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")); // Light red
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convert connection status to border color
    /// </summary>
    public class ConnectionStatusToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() == "allowed"
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC")) // Green border
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")); // Red border
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convert connection status to text color
    /// </summary>
    public class ConnectionStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() == "allowed"
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")) // Green text
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")); // Red text
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
