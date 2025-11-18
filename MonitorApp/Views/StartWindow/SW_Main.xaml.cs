using System.Windows;
using System.Threading.Tasks;

namespace MonitorApp.Views.StartWindow
{
    public partial class SW_Main : Window
    {
        public SW_Main()
        {
            InitializeComponent();

            // Khi màn hình welcome load xong thì gọi hàm này
            Loaded += SW_Main_Loaded;
        }

        // Đợi 5 giây rồi tự chuyển sang màn MainWindow
        private async void SW_Main_Loaded(object sender, RoutedEventArgs e)
        {
            // Đợi 5 giây (5000 mili-giây)
            await Task.Delay(5000);

            // Mở màn hình chính (MainWindow)
            var main = new MonitorApp.Views.Windows.MainWindow();
            main.Show();

            // Đóng màn hình welcome
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
