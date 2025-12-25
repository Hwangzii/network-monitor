using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Data;

namespace MonitorApp.Converters
{
    // base64 data:image/svg+xml;base64,...  ->  absolute temp .svg file path
    public class Base64SvgToTempFileConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s)) return null;

            try
            {
                var comma = s.IndexOf(',');
                if (comma >= 0) s = s[(comma + 1)..];

                var bytes = System.Convert.FromBase64String(s);

                // IMPORTANT: SVG is text; keep bytes as-is, but ensure file is .svg
                var tempDir = Path.Combine(Path.GetTempPath(), "MonitorAppSvg");
                Directory.CreateDirectory(tempDir);

                // Use stable name per content to avoid writing many files
                var hash = System.Convert.ToHexString(
                System.Security.Cryptography.SHA1.HashData(bytes)
            );

                var path = Path.Combine(tempDir, $"{hash}.svg");

                if (!File.Exists(path))
                {
                    // write as UTF8 text
                    var svgText = Encoding.UTF8.GetString(bytes);
                    File.WriteAllText(path, svgText, Encoding.UTF8);
                }

                return new Uri(path, UriKind.Absolute);
                // absolute path string
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
