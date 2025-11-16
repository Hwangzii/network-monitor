using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MonitorApp.Converters
{
    /// <summary>
    /// Từ chuỗi số liệu (IEnumerable<double>) + (width,height)
    /// sinh Geometry cho đường (và optional area fill).
    /// ConverterParameter: "maxPoints|maxValue|area"
    ///  - maxPoints: số điểm tối đa (mặc định 60)
    ///  - maxValue : giá trị Y max (mặc định 100)
    ///  - area     : true/false (mặc định true) -> khép kín để Fill
    /// </summary>
    public class SeriesToGeometryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return Geometry.Empty;

            var data = values[0] as IEnumerable<double>;
            if (data == null) return Geometry.Empty;

            double width = values[1] is double w ? w : 0;
            double height = values[2] is double h ? h : 0;
            if (width <= 0 || height <= 0) return Geometry.Empty;

            int maxPoints = 60;
            double maxValue = 100.0;
            bool area = true;

            if (parameter is string p && !string.IsNullOrWhiteSpace(p))
            {
                var parts = p.Split('|');
                if (parts.Length > 0 && int.TryParse(parts[0], out var mp)) maxPoints = mp;
                if (parts.Length > 1 && double.TryParse(parts[1], out var mv)) maxValue = mv;
                if (parts.Length > 2 && bool.TryParse(parts[2], out var ar)) area = ar;
            }

            var arr = data as IList<double> ?? data.ToList();
            if (arr.Count == 0) return Geometry.Empty;

            int start = Math.Max(0, arr.Count - maxPoints);
            var slice = arr.Skip(start).Take(maxPoints).ToList();
            int n = slice.Count;
            if (n < 2) return Geometry.Empty;

            // Tính theo số điểm THỰC TẾ (n)
            double step = (n > 1) ? (width / (n - 1)) : width;

            var geom = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (var ctx = geom.Open())
            {
                double y0 = height - (Clamp(slice[0], 0, maxValue) / maxValue) * height;
                if (area)
                {
                    // bắt đầu từ đáy trái để có Fill
                    ctx.BeginFigure(new Point(0, height), isFilled: true, isClosed: true);
                    ctx.LineTo(new Point(0, y0), isStroked: true, isSmoothJoin: false);
                }
                else
                {
                    ctx.BeginFigure(new Point(0, y0), isFilled: false, isClosed: false);
                }

                for (int i = 1; i < n; i++)
                {
                    double x = i * step;
                    double y = height - (Clamp(slice[i], 0, maxValue) / maxValue) * height;
                    ctx.LineTo(new Point(x, y), isStroked: true, isSmoothJoin: false);
                }

                if (area)
                {
                    // ĐÓNG VỀ ĐÁY TẠI x CUỐI CÙNG (n-1)*step
                    ctx.LineTo(new Point((n - 1) * step, height), isStroked: false, isSmoothJoin: false);
                }
            }
            geom.Freeze();
            return geom;
        }

        private static double Clamp(double v, double lo, double hi)
            => v < lo ? lo : (v > hi ? hi : v);


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;

    }
}
