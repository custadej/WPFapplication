using System.Windows;
using System.Windows.Media;

namespace TakojsnjeSporocanje
{
    public partial class ModernDialogWindow : Window
    {
        public string DialogTitle { get; }
        public string DialogMessage { get; }

        public ModernDialogWindow(string title, string message, bool showCancelButton, string confirmText = "V redu", bool destructive = false)
        {
            InitializeComponent();
            DialogTitle = title;
            DialogMessage = message;
            DataContext = this;

            ConfirmButton.Content = confirmText;
            CancelButton.Visibility = showCancelButton ? Visibility.Visible : Visibility.Collapsed;

            if (destructive)
            {
                ConfirmButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC2626"));
            }
        }

        public static bool ShowConfirmation(Window owner, string title, string message, string confirmText = "Potrdi", bool destructive = false)
        {
            ModernDialogWindow dialog = new ModernDialogWindow(title, message, true, confirmText, destructive)
            {
                Owner = owner
            };

            return dialog.ShowDialog() == true;
        }

        public static void ShowInfo(Window owner, string title, string message)
        {
            ModernDialogWindow dialog = new ModernDialogWindow(title, message, false)
            {
                Owner = owner
            };

            dialog.ShowDialog();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
