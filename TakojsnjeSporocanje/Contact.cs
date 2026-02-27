using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TakojsnjeSporocanje
{
    public class Contact : INotifyPropertyChanged
    {
        private string nickname;
        private string status;
        private string email;
        private string conversation;
        private string imagePath;
        private string lastActive;

        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
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

        public string Conversation
        {
            get => conversation;
            set { conversation = value; OnPropertyChanged(); }
        }

        public string ImagePath
        {
            get => imagePath;
            set { imagePath = value; OnPropertyChanged(); }
        }

        public string LastActive
        {
            get => lastActive;
            set { lastActive = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}