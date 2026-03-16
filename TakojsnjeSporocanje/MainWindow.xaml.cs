using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace TakojsnjeSporocanje
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string DefaultDataFileName = "chat-data.xml";

        public ChatData AppData { get; set; }

        public ObservableCollection<ChatMessage> CurrentConversationMessages { get; } = new ObservableCollection<ChatMessage>();

        private Contact selectedContact;
        private Contact subscribedContact;
        private bool isUpdatingData;

        public Contact SelectedContact
        {
            get => selectedContact;
            set
            {
                if (subscribedContact != null)
                {
                    subscribedContact.PropertyChanged -= SelectedContact_PropertyChanged;
                }

                selectedContact = value;
                subscribedContact = value;

                if (subscribedContact != null)
                {
                    subscribedContact.PropertyChanged += SelectedContact_PropertyChanged;
                }

                RefreshConversationMessages();
                OnPropertyChanged();
                UpdateContactMenuState();
            }
        }

        public ObservableCollection<string> UserStatuses { get; } = new ObservableCollection<string>
        {
            "Online",
            "Away",
            "Busy"
        };

        public string ContactCountText => GetContactCountText(AppData?.Contacts.Count ?? 0);

        public MainWindow()
        {
            InitializeComponent();

            AppData = CreateDefaultData();
            AttachDataHandlers(AppData);
            LoadStartupData();

            DataContext = this;
            SelectedContact = AppData.Contacts.Count > 0 ? AppData.Contacts[0] : null;
            UpdateContactMenuState();
            OnPropertyChanged(nameof(ContactCountText));
        }

        private void MenuImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Uvozi XML podatke",
                Filter = "XML datoteke (*.xml)|*.xml|Vse datoteke (*.*)|*.*",
                DefaultExt = ".xml"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ChatData importedData = LoadDataFromFile(dialog.FileName);
                ReplaceAppData(importedData);
                SaveDefaultDataSilently();
                ModernDialogWindow.ShowInfo(this, "Uvoz uspesen", "Podatki so bili uspesno uvozeni iz XML datoteke.");
            }
            catch
            {
                ModernDialogWindow.ShowInfo(this, "Napaka pri uvozu", "XML datoteke ni bilo mogoce uvoziti.");
            }
        }

        private void MenuExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Izvozi XML podatke",
                Filter = "XML datoteke (*.xml)|*.xml|Vse datoteke (*.*)|*.*",
                DefaultExt = ".xml",
                FileName = DefaultDataFileName
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                SaveDataToFile(dialog.FileName);
                ModernDialogWindow.ShowInfo(this, "Izvoz uspesen", "Podatki so bili uspesno izvozeni v XML datoteko.");
            }
            catch
            {
                ModernDialogWindow.ShowInfo(this, "Napaka pri izvozu", "XML datoteke ni bilo mogoce shraniti.");
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuAddContact_Click(object sender, RoutedEventArgs e)
        {
            ContactWindow dialog = new ContactWindow("Dodaj stik")
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            Contact newContact = new Contact
            {
                Nickname = dialog.EditableContact.Nickname,
                LastName = dialog.EditableContact.LastName,
                Status = dialog.EditableContact.Status,
                Email = dialog.EditableContact.Email,
                Phone = dialog.EditableContact.Phone,
                Conversation = string.IsNullOrWhiteSpace(dialog.EditableContact.Conversation)
                    ? $"{dialog.EditableContact.Nickname}: Zivjo!\n"
                    : dialog.EditableContact.Conversation,
                ImagePath = dialog.EditableContact.ImagePath,
                LastActive = dialog.EditableContact.LastActive
            };

            AppData.Contacts.Add(newContact);
            SelectedContact = newContact;
        }

        private void MenuRemoveContact_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null)
            {
                ModernDialogWindow.ShowInfo(this, "Ni izbranega stika", "Najprej izberi stik, ki ga zelis odstraniti.");
                return;
            }

            Contact contactToRemove = SelectedContact;
            bool confirmed = ModernDialogWindow.ShowConfirmation(
                this,
                "Odstrani stik",
                $"Ali si preprican, da zelis odstraniti stik {contactToRemove.Nickname}?",
                "Izbrisi",
                true);

            if (!confirmed)
            {
                return;
            }

            AppData.Contacts.Remove(contactToRemove);
            SelectedContact = null;
        }

        private void MenuEditContact_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null)
            {
                ModernDialogWindow.ShowInfo(this, "Ni izbranega stika", "Najprej izberi stik, ki ga zelis urediti.");
                return;
            }

            ContactWindow dialog = new ContactWindow("Uredi stik", SelectedContact)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            SelectedContact.Nickname = dialog.EditableContact.Nickname;
            SelectedContact.LastName = dialog.EditableContact.LastName;
            SelectedContact.Status = dialog.EditableContact.Status;
            SelectedContact.Email = dialog.EditableContact.Email;
            SelectedContact.Phone = dialog.EditableContact.Phone;
            SelectedContact.ImagePath = dialog.EditableContact.ImagePath;
            SelectedContact.LastActive = dialog.EditableContact.LastActive;
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow dialog = new SettingsWindow(AppData.CurrentUser)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            AppData.CurrentUser.Nickname = dialog.EditableProfile.Nickname;
            AppData.CurrentUser.LastName = dialog.EditableProfile.LastName;
            AppData.CurrentUser.Email = dialog.EditableProfile.Email;
            AppData.CurrentUser.Phone = dialog.EditableProfile.Phone;
            AppData.CurrentUser.ImagePath = dialog.EditableProfile.ImagePath;
            AppData.CurrentUser.About = dialog.EditableProfile.About;
            AppData.CurrentUser.City = dialog.EditableProfile.City;
            AppData.CurrentUser.Country = dialog.EditableProfile.Country;
            RefreshConversationMessages();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null || string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                return;
            }

            string message = MessageTextBox.Text.Trim();
            SelectedContact.Conversation += AppData.CurrentUser.Nickname + ": " + message + "\n";

            string response = message.ToLower().Contains("kako")
                ? "Super sem :)"
                : "OK :)";

            SelectedContact.Conversation += SelectedContact.Nickname + ": " + response + "\n";

            MessageTextBox.Clear();
            Dispatcher.BeginInvoke(new Action(() => ChatScrollViewer.ScrollToEnd()), DispatcherPriority.Background);
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text += " :)";
        }

        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ComboBox comboBox && !comboBox.IsDropDownOpen)
            {
                e.Handled = true;
            }
        }

        private void UpdateContactMenuState()
        {
            bool hasSelectedContact = SelectedContact != null;
            RemoveContactMenuItem.IsEnabled = hasSelectedContact;
            EditContactMenuItem.IsEnabled = hasSelectedContact;
        }

        private void Contacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Contact oldContact in e.OldItems)
                {
                    oldContact.PropertyChanged -= Contact_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (Contact newContact in e.NewItems)
                {
                    newContact.PropertyChanged += Contact_PropertyChanged;
                }
            }

            OnPropertyChanged(nameof(ContactCountText));
            SaveDefaultDataSilently();
        }

        private void CurrentUser_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveDefaultDataSilently();
        }

        private void Contact_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveDefaultDataSilently();
        }

        private void SelectedContact_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Contact.Conversation))
            {
                RefreshConversationMessages();
                Dispatcher.BeginInvoke(new Action(() => ChatScrollViewer.ScrollToEnd()), DispatcherPriority.Background);
            }
        }

        private void RefreshConversationMessages()
        {
            CurrentConversationMessages.Clear();

            if (SelectedContact == null || string.IsNullOrWhiteSpace(SelectedContact.Conversation))
            {
                return;
            }

            string[] lines = SelectedContact.Conversation.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
                {
                    CurrentConversationMessages.Add(new ChatMessage
                    {
                        Text = line.Trim(),
                        IsCurrentUser = false
                    });
                    continue;
                }

                string senderName = line.Substring(0, separatorIndex).Trim();
                string text = line.Substring(separatorIndex + 1).Trim();

                CurrentConversationMessages.Add(new ChatMessage
                {
                    Text = text,
                    IsCurrentUser = senderName == AppData.CurrentUser.Nickname
                });
            }
        }

        private ChatData CreateDefaultData()
        {
            ChatData data = new ChatData
            {
                CurrentUser = new UserProfile
                {
                    Nickname = "Tadej",
                    LastName = "Čuš",
                    Status = "Online",
                    Email = "cus.tadej07@gmail.com",
                    Phone = "070 343 488",
                    ImagePath = "Images/user.png",
                    About = "Dijak",
                    City = "Kidričevo",
                    Country = "Slovenija"
                }
            };

            data.Contacts.Add(new Contact
            {
                Nickname = "Niko",
                LastName = "Cvetko",
                Status = "Online",
                Email = "niko@gmail.com",
                Phone = "041 420 067",
                Conversation = "Niko: Živjo!\n",
                ImagePath = "Images/user.png",
                LastActive = "Danes"
            });

            data.Contacts.Add(new Contact
            {
                Nickname = "Aljaž",
                LastName = "Šešo",
                Status = "Away",
                Email = "aljaz@gmail.com",
                Phone = "041 222 222",
                Conversation = "Aljaž: Hej!\n",
                ImagePath = "Images/user.png",
                LastActive = "Včeraj"
            });

            return data;
        }

        private void LoadStartupData()
        {
            string defaultPath = GetDefaultDataFilePath();

            if (File.Exists(defaultPath))
            {
                try
                {
                    ChatData loadedData = LoadDataFromFile(defaultPath);
                    ReplaceAppData(loadedData);
                    return;
                }
                catch
                {
                    ModernDialogWindow.ShowInfo(this, "Napaka pri nalaganju", "Privzete XML datoteke ni bilo mogoce prebrati. Nalozeni so zacetni podatki.");
                }
            }

            SaveDefaultDataSilently();
        }

        private void ReplaceAppData(ChatData newData)
        {
            isUpdatingData = true;

            if (AppData != null)
            {
                DetachDataHandlers(AppData);
            }

            AppData = NormalizeData(newData);
            AttachDataHandlers(AppData);

            OnPropertyChanged(nameof(AppData));
            OnPropertyChanged(nameof(ContactCountText));

            SelectedContact = AppData.Contacts.Count > 0 ? AppData.Contacts[0] : null;

            isUpdatingData = false;
        }

        private void AttachDataHandlers(ChatData data)
        {
            if (data == null)
            {
                return;
            }

            data.Contacts.CollectionChanged += Contacts_CollectionChanged;

            foreach (Contact contact in data.Contacts)
            {
                contact.PropertyChanged += Contact_PropertyChanged;
            }

            if (data.CurrentUser != null)
            {
                data.CurrentUser.PropertyChanged += CurrentUser_PropertyChanged;
            }
        }

        private void DetachDataHandlers(ChatData data)
        {
            if (data == null)
            {
                return;
            }

            data.Contacts.CollectionChanged -= Contacts_CollectionChanged;

            foreach (Contact contact in data.Contacts)
            {
                contact.PropertyChanged -= Contact_PropertyChanged;
            }

            if (data.CurrentUser != null)
            {
                data.CurrentUser.PropertyChanged -= CurrentUser_PropertyChanged;
            }
        }

        private ChatData NormalizeData(ChatData data)
        {
            if (data == null)
            {
                return CreateDefaultData();
            }

            if (data.CurrentUser == null)
            {
                data.CurrentUser = new UserProfile();
            }

            if (data.Contacts == null)
            {
                data.Contacts = new ObservableCollection<Contact>();
            }

            return data;
        }

        private ChatData LoadDataFromFile(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ChatData));

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            ChatData loadedData = serializer.Deserialize(stream) as ChatData;
            return NormalizeData(loadedData);
        }

        private void SaveDataToFile(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ChatData));

            using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            serializer.Serialize(stream, AppData);
        }

        private void SaveDefaultDataSilently()
        {
            if (isUpdatingData || AppData == null)
            {
                return;
            }

            try
            {
                SaveDataToFile(GetDefaultDataFilePath());
            }
            catch
            {
            }
        }

        private string GetDefaultDataFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultDataFileName);
        }

        private static string GetContactCountText(int count)
        {
            int lastTwoDigits = count % 100;
            int lastDigit = count % 10;

            if (lastTwoDigits is >= 11 and <= 14)
            {
                return $"{count} oseb";
            }

            return lastDigit switch
            {
                1 => $"{count} oseba",
                2 => $"{count} osebi",
                3 or 4 => $"{count} osebe",
                _ => $"{count} oseb"
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
