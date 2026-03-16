using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TakojsnjeSporocanje
{
    public class ChatData : INotifyPropertyChanged
    {
        private UserProfile currentUser;

        public ObservableCollection<Contact> Contacts { get; set; } = new ObservableCollection<Contact>();

        public UserProfile CurrentUser
        {
            get => currentUser;
            set { currentUser = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
