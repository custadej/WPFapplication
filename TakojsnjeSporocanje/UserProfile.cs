using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TakojsnjeSporocanje
{
    public class UserProfile : INotifyPropertyChanged
    {
        private string nickname;
        private string lastName;
        private string status;
        private string email;
        private string phone;
        private string imagePath;
        private string about;
        private string city;
        private string country;

        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        public string LastName
        {
            get => lastName;
            set { lastName = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => email;
            set { email = value; OnPropertyChanged(); }
        }

        public string Phone
        {
            get => phone;
            set { phone = value; OnPropertyChanged(); }
        }

        public string ImagePath
        {
            get => imagePath;
            set { imagePath = value; OnPropertyChanged(); }
        }

        public string About
        {
            get => about;
            set { about = value; OnPropertyChanged(); }
        }

        public string City
        {
            get => city;
            set { city = value; OnPropertyChanged(); }
        }

        public string Country
        {
            get => country;
            set { country = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
