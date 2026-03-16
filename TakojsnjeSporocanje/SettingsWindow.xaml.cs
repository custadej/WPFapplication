using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TakojsnjeSporocanje
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public UserProfile EditableProfile { get; }

        public string ImageButtonText => IsDefaultImage(EditableProfile.ImagePath) ? "Dodaj sliko" : "Spremeni sliko";

        public SettingsWindow(UserProfile userProfile)
        {
            InitializeComponent();
            EditableProfile = new UserProfile
            {
                Nickname = userProfile.Nickname,
                LastName = userProfile.LastName,
                Email = userProfile.Email,
                Phone = userProfile.Phone,
                Status = userProfile.Status,
                ImagePath = userProfile.ImagePath,
                About = userProfile.About,
                City = userProfile.City,
                Country = userProfile.Country
            };

            DataContext = this;
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Izberi prikazno sliko",
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
                EditableProfile.ImagePath = cropWindow.CroppedImagePath;
                OnPropertyChanged(nameof(ImageButtonText));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditableProfile.Nickname))
            {
                ModernDialogWindow.ShowInfo(this, "Manjka vzdevek", "Vnesi vzdevek, preden shraniš spremembe.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditableProfile.Email))
            {
                ModernDialogWindow.ShowInfo(this, "Manjka e-pošta", "Vnesi e-poštni naslov, preden shraniš spremembe.");
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
