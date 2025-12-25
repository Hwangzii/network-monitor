using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MonitorApp.Helpers
{
    public static class ImageHelper
    {
        public static ImageSource? FromBase64DataUri(string? dataUri)
        {
            if (string.IsNullOrWhiteSpace(dataUri)) return null;

            // accept "data:image/png;base64,...." hoặc raw base64
            var comma = dataUri.IndexOf(',');
            var b64 = comma >= 0 ? dataUri[(comma + 1)..] : dataUri;

            try
            {
                var bytes = Convert.FromBase64String(b64);
                using var ms = new MemoryStream(bytes);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        public static ImageSource? FromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(url, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }
    }
}
