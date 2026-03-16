using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TakojsnjeSporocanje
{
    public partial class ContactWindow : Window, INotifyPropertyChanged
    {
        public Contact EditableContact { get; }

        public string ImageButtonText => IsDefaultImage(EditableContact.ImagePath) ? "Dodaj sliko" : "Spremeni sliko";

        public ContactWindow(string windowTitle, Contact contact = null)
        {
            InitializeComponent();
            Title = windowTitle;
            EditableContact = new Contact
            {
                Nickname = contact?.Nickname ?? string.Empty,
                LastName = contact?.LastName ?? string.Empty,
                Email = contact?.Email ?? string.Empty,
                Phone = contact?.Phone ?? string.Empty,
                Status = contact?.Status ?? "Online",
                LastActive = contact?.LastActive ?? "Danes",
                Conversation = contact?.Conversation ?? string.Empty,
                ImagePath = string.IsNullOrWhiteSpace(contact?.ImagePath) ? "Images/user.png" : contact.ImagePath
            };

            DataContext = this;
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Izberi sliko stika",
                Filter = "Slikovne datoteke|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            ImageCropWindow cropWindow = new ImageCropWindow(dialog.FileName)
            {
                Owner = this
            };

            if (cropWindow.ShowDialog() == true)
            {
                EditableContact.ImagePath = cropWindow.CroppedImagePath;
                OnPropertyChanged(nameof(ImageButtonText));
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditableContact.Nickname))
            {
                ModernDialogWindow.ShowInfo(this, "Manjka vzdevek", "Vnesi vzdevek stika, preden nadaljuješ.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditableContact.Email))
            {
                ModernDialogWindow.ShowInfo(this, "Manjka e-pošta", "Vnesi e-pošto stika, preden nadaljuješ.");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static bool IsDefaultImage(string imagePath)
        {
            return string.IsNullOrWhiteSpace(imagePath) || imagePath == "Images/user.png";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
