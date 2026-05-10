using System;
using System.Windows;

namespace TakojsnjeSporocanje
{
    public partial class NetworkWindow : Window
    {
        public ChatNetworkService NetworkService { get; private set; }

        public NetworkWindow(ChatNetworkService existingService = null)
        {
            InitializeComponent();
            NetworkService = existingService;
            LocalIPBox.Text = new ChatNetworkService().LocalIPAddress;
            UpdateConnectedUI(existingService?.IsConnected == true);
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (ServerPanel == null) return;
            ServerPanel.Visibility = ServerRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ClientPanel.Visibility = ClientRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SetStatus("Vzpostavljam povezavo...", "#FF1E40AF", "#FFEFF6FF");

            ChatNetworkService service = new ChatNetworkService();

            try
            {
                if (ServerRadio.IsChecked == true)
                {
                    if (!int.TryParse(ServerPortBox.Text.Trim(), out int port) || port < 1 || port > 65535)
                    {
                        SetStatus("Neveljavna številka vrat.", "#FF991B1B", "#FFFEF2F2");
                        ConnectButton.IsEnabled = true;
                        CancelButton.IsEnabled = true;
                        return;
                    }

                    SetStatus($"Čakam na odjemalca na vratih {port}...", "#FF1E40AF", "#FFEFF6FF");
                    _ = service.StartServerAsync(port);
                }
                else
                {
                    string host = ServerIPBox.Text.Trim();
                    if (!int.TryParse(ClientPortBox.Text.Trim(), out int port) || port < 1 || port > 65535)
                    {
                        SetStatus("Neveljavna številka vrat.", "#FF991B1B", "#FFFEF2F2");
                        ConnectButton.IsEnabled = true;
                        CancelButton.IsEnabled = true;
                        return;
                    }

                    await service.ConnectAsync(host, port);
                }

                NetworkService = service;
                UpdateConnectedUI(true);
                SetStatus("Uspešno povezano!", "#FF166534", "#FFF0FDF4");
                ConnectButton.Content = "Prekini";
                ConnectButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                service.Dispose();
                SetStatus($"Napaka: {ex.Message}", "#FF991B1B", "#FFFEF2F2");
                ConnectButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SetStatus(string message, string foreground, string background)
        {
            StatusBorder.Visibility = Visibility.Visible;
            StatusText.Text = message;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(foreground));
            StatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(background));
        }

        private void UpdateConnectedUI(bool connected)
        {
            ServerRadio.IsEnabled = !connected;
            ClientRadio.IsEnabled = !connected;
        }
    }
}
