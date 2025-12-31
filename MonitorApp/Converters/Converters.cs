// file: frontend\MonitorApp\Views\Pages\Converters.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MonitorApp.Views.Pages
{
    /// <summary>
    /// Chuyển StatusText thành màu: nếu chứa "completed" → xanh lá, còn lại → xám
    /// </summary>
    public class StatusToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is string status &&
                status.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
            }

            return new SolidColorBrush(Color.FromRgb(107, 114, 128)); // #6B7280
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Hiện icon check chỉ khi StatusText chứa "completed"
    /// </summary>
    public class CompletedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status &&
                status.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}