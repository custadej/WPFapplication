using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TakojsnjeSporocanje
{
    public class Contact : INotifyPropertyChanged
    {
        private string nickname;
        private string lastName;
        private string status;
        private string email;
        private string phone;
        private string conversation;
        private string imagePath;
        private string lastActive;

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

        public string Conversation
        {
            get => conversation;
            set
            {
                conversation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastMessagePreview));
            }
        }

        public string LastMessagePreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(conversation))
                    return string.Empty;

                string[] lines = conversation.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0)
                    return string.Empty;

                string lastLine = lines[lines.Length - 1];
                int separatorIndex = lastLine.IndexOf(':');
                if (separatorIndex >= 0 && separatorIndex < lastLine.Length - 1)
                    return lastLine.Substring(separatorIndex + 1).Trim();

                return lastLine.Trim();
            }
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
