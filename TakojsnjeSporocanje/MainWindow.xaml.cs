using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TakojsnjeSporocanje
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Contact> Contacts { get; set; }

        private Contact selectedContact;
        public Contact SelectedContact
        {
            get => selectedContact;
            set
            {
                selectedContact = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Contacts = new ObservableCollection<Contact>
            {
                new Contact
                {
                    Nickname = "Miha",
                    Status = "Online",
                    Email = "miha@gmail.com",
                    Conversation = "Miha: Živjo!\n",
                    ImagePath = "Images/user.png",
                    LastActive = "Danes"
                },
                new Contact
                {
                    Nickname = "Nina",
                    Status = "Away",
                    Email = "nina@gmail.com",
                    Conversation = "Nina: Hej!\n",
                    ImagePath = "Images/user.png",
                    LastActive = "Včeraj"
                }
            };

            DataContext = this;
            SelectedContact = Contacts[0];
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuAddContact_Click(object sender, RoutedEventArgs e)
        {
            InputDialog dialog =
                new InputDialog("Vnesite ime novega stika:", "NovStik");

            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    Contacts.Add(new Contact
                    {
                        Nickname = dialog.ResponseText,
                        Status = "Online",
                        Email = "",
                        Conversation = dialog.ResponseText + ": Živjo!\n",
                        ImagePath = "Images/user.png",
                        LastActive = "Danes"
                    });
                }
            }
        }

        private void MenuRemoveContact_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact != null)
                Contacts.Remove(SelectedContact);
        }

        private void MenuEditContact_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null) return;

            InputDialog dialog =
                new InputDialog("Novo ime stika:", SelectedContact.Nickname);

            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    SelectedContact.Nickname = dialog.ResponseText;
                }
            }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nastavitve pridejo kasneje.");
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null) return;
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text)) return;

            string message = MessageTextBox.Text;

            SelectedContact.Conversation +=
                NicknameTextBox.Text + ": " + message + "\n";

            string response = message.ToLower().Contains("kako")
                ? "Super sem 😄"
                : "OK 👍";

            SelectedContact.Conversation +=
                SelectedContact.Nickname + ": " + response + "\n";

            MessageTextBox.Clear();
            ChatScrollViewer.ScrollToEnd();
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text += "😊";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}