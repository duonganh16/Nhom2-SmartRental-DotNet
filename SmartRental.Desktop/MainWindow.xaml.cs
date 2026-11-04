
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SmartRental.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly DesktopHost _desktopHost = new();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += Window_Loaded;
            Closing += Window_Closing;
        }

        private async void Window_Loaded(
            object sender, RoutedEventArgs e)
        {
            try
            {
                Title = "SmartRental - Đang khởi động...";

                await _desktopHost.StartAsync();

                await SmartRentalBrowser.EnsureCoreWebView2Async();

                SmartRentalBrowser.Source =
                    new Uri("http://127.0.0.1:5088");

                Title = "SmartRental - Hệ thống quản lý phòng trọ";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể khởi động SmartRental:\n\n" + ex.Message,
                    "Lỗi khởi động",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Window_Closing(
            object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _desktopHost.Stop();
        }
    }
}
