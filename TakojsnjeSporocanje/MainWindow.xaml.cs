using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace TakojsnjeSporocanje
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string DefaultDataFileName = "chat-data.xml";

        private readonly DispatcherTimer autoSaveTimer = new DispatcherTimer();
        private ChatNetworkService networkService;

        public ChatData AppData { get; set; }

        public ObservableCollection<ChatMessage> CurrentConversationMessages { get; } = new ObservableCollection<ChatMessage>();

        private Contact selectedContact;
        private Contact subscribedContact;
        private bool isUpdatingData;
        private string composerMessageText = string.Empty;

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

        public string AutoSaveStatusText
        {
            get
            {
                if (AppData?.CurrentUser == null || !AppData.CurrentUser.AutoSaveEnabled)
                {
                    return "Samodejno shranjevanje: izklopljeno";
                }

                return $"Samodejno shranjevanje: vsakih {AppData.CurrentUser.AutoSaveIntervalMinutes} min";
            }
        }

        private bool isNetworkConnected;
        public bool IsNetworkConnected
        {
            get => isNetworkConnected;
            set { isNetworkConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(NetworkStatusText)); }
        }

        public string NetworkStatusText => isNetworkConnected ? "Omrežje: Povezano" : "Omrežje: Ni povezave";

        public string ComposerMessageText
        {
            get => composerMessageText;
            set
            {
                composerMessageText = value;
                OnPropertyChanged();
            }
        }

        // Contact search
        private string contactSearchText = string.Empty;
        public string ContactSearchText
        {
            get => contactSearchText;
            set
            {
                contactSearchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSearchText));
                ContactsView?.Refresh();
            }
        }
        public bool HasSearchText => !string.IsNullOrEmpty(contactSearchText);

        // Filtered contacts view
        private ICollectionView contactsView;
        public ICollectionView ContactsView
        {
            get => contactsView;
            private set { contactsView = value; OnPropertyChanged(); }
        }

        private void RebuildContactsView()
        {
            ContactsView = CollectionViewSource.GetDefaultView(AppData.Contacts);
            ContactsView.Filter = FilterContact;
        }

        private bool FilterContact(object obj)
        {
            if (string.IsNullOrWhiteSpace(contactSearchText)) return true;
            if (obj is Contact c)
                return c.Nickname.Contains(contactSearchText, StringComparison.OrdinalIgnoreCase) ||
                       (c.Email?.Contains(contactSearchText, StringComparison.OrdinalIgnoreCase) == true);
            return false;
        }

        // Sidebar tab
        private bool isDiscoverTabActive;
        public bool IsDiscoverTabActive
        {
            get => isDiscoverTabActive;
            set { isDiscoverTabActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContactsTabActive)); }
        }
        public bool IsContactsTabActive => !isDiscoverTabActive;

        public ObservableCollection<SuggestedFriend> SuggestedFriends { get; } = BuildSuggestedFriends();

        private static ObservableCollection<SuggestedFriend> BuildSuggestedFriends()
        {
            return new ObservableCollection<SuggestedFriend>
            {
                new() { Nickname = "Maja Kovač",    Initials = "MK", MutualInfo = "23 skupnih prijateljev", Source = "Iz imenika",   AvatarColor = System.Windows.Media.Color.FromRgb(0xEC, 0x48, 0x99) },
                new() { Nickname = "Luka Novak",    Initials = "LN", MutualInfo = "12 skupnih prijateljev", Source = "Morda poznaš", AvatarColor = System.Windows.Media.Color.FromRgb(0x22, 0xC5, 0x5E) },
                new() { Nickname = "Sara Potočnik", Initials = "SP", MutualInfo = "7 skupnih prijateljev",  Source = "Nearby",       AvatarColor = System.Windows.Media.Color.FromRgb(0xF9, 0x73, 0x16) },
                new() { Nickname = "Jaka Horvat",   Initials = "JH", MutualInfo = "Ni skupnih prijateljev", Source = "Iz imenika",   AvatarColor = System.Windows.Media.Color.FromRgb(0x06, 0xB6, 0xD4) },
                new() { Nickname = "Ana Zupančič",  Initials = "AZ", MutualInfo = "34 skupnih prijateljev", Source = "Morda poznaš", AvatarColor = System.Windows.Media.Color.FromRgb(0xA8, 0x55, 0xF7) },
                new() { Nickname = "Rok Petrič",    Initials = "RP", MutualInfo = "5 skupnih prijateljev",  Source = "Nearby",       AvatarColor = System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81) },
                new() { Nickname = "Eva Kos",       Initials = "EK", MutualInfo = "18 skupnih prijateljev", Source = "Iz imenika",   AvatarColor = System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44) },
                new() { Nickname = "Miha Štefan",   Initials = "MŠ", MutualInfo = "2 skupna prijatelja",    Source = "Morda poznaš", AvatarColor = System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B) },
            };
        }

        // Layout toggle
        private bool isCompactView;
        public bool IsCompactView
        {
            get => isCompactView;
            set { isCompactView = value; OnPropertyChanged(); IsDefaultViewChecked = !value; }
        }

        private bool isDefaultViewChecked = true;
        public bool IsDefaultViewChecked
        {
            get => isDefaultViewChecked;
            set { isDefaultViewChecked = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            InitializeComponent();

            AppData = CreateDefaultData();
            AttachDataHandlers(AppData);
            LoadStartupData();
            RebuildContactsView();

            DataContext = this;
            SelectedContact = AppData.Contacts.Count > 0 ? AppData.Contacts[0] : null;
            UpdateContactMenuState();
            OnPropertyChanged(nameof(ContactCountText));

            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            ApplyAutoSaveSettings();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            SaveDefaultDataSilently();
        }

        protected override void OnClosed(EventArgs e)
        {
            autoSaveTimer.Stop();
            networkService?.Dispose();
            base.OnClosed(e);
        }

        private void ApplyAutoSaveSettings()
        {
            autoSaveTimer.Stop();

            if (AppData?.CurrentUser == null || !AppData.CurrentUser.AutoSaveEnabled)
            {
                return;
            }

            int minutes = Math.Max(1, AppData.CurrentUser.AutoSaveIntervalMinutes);
            autoSaveTimer.Interval = TimeSpan.FromMinutes(minutes);
            autoSaveTimer.Start();
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
            AppData.CurrentUser.AutoSaveEnabled = dialog.EditableProfile.AutoSaveEnabled;
            AppData.CurrentUser.AutoSaveIntervalMinutes = dialog.EditableProfile.AutoSaveIntervalMinutes;
            ApplyAutoSaveSettings();
            OnPropertyChanged(nameof(AutoSaveStatusText));
            RefreshConversationMessages();
        }

        private void MenuNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworkWindow dialog = new NetworkWindow(networkService) { Owner = this };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            networkService?.Dispose();
            networkService = dialog.NetworkService;

            if (networkService != null)
            {
                networkService.MessageReceived += Network_MessageReceived;
                networkService.Disconnected += Network_Disconnected;
                NetworkConnectMenuItem.IsEnabled = false;
                NetworkDisconnectMenuItem.IsEnabled = true;
                IsNetworkConnected = true;
                ModernDialogWindow.ShowInfo(this, "Omrežje", "Omrežna povezava je vzpostavljena. Sporočila bodo posredovana sogovorniku.");
            }
        }

        private void MenuNetworkDisconnect_Click(object sender, RoutedEventArgs e)
        {
            networkService?.Dispose();
            networkService = null;
            NetworkConnectMenuItem.IsEnabled = true;
            NetworkDisconnectMenuItem.IsEnabled = false;
            IsNetworkConnected = false;
            ModernDialogWindow.ShowInfo(this, "Omrežje", "Omrežna povezava je prekinjena.");
        }

        private void Network_MessageReceived(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (SelectedContact == null)
                {
                    return;
                }

                SelectedContact.Conversation += SelectedContact.Nickname + ": " + message + "\n";
            });
        }

        private void Network_Disconnected(string reason)
        {
            Dispatcher.Invoke(() =>
            {
                networkService = null;
                NetworkConnectMenuItem.IsEnabled = true;
                NetworkDisconnectMenuItem.IsEnabled = false;
                IsNetworkConnected = false;
                ModernDialogWindow.ShowInfo(this, "Omrežje prekinjeno", reason);
            });
        }

        private void MessageComposer_SendMessageRequested(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null || string.IsNullOrWhiteSpace(ComposerMessageText))
            {
                return;
            }

            string message = ComposerMessageText.Trim();
            SelectedContact.Conversation += AppData.CurrentUser.Nickname + ": " + message + "\n";

            if (networkService?.IsConnected == true)
            {
                networkService.SendMessage(message);
            }
            else
            {
                SendBotReply(message);
            }

            ComposerMessageText = string.Empty;
            MessageComposer.ClearMessage();
            MessageComposer.FocusInput();
            Dispatcher.BeginInvoke(new Action(() => ChatScrollViewer.ScrollToEnd()), DispatcherPriority.Background);
        }

        private void MenuPrivzeto_Click(object sender, RoutedEventArgs e)
        {
            IsCompactView = false;
            MenuPrivzeto.IsChecked = true;
            MenuAlternativno.IsChecked = false;
        }

        private void MenuAlternativno_Click(object sender, RoutedEventArgs e)
        {
            IsCompactView = true;
            MenuAlternativno.IsChecked = true;
            MenuPrivzeto.IsChecked = false;
        }

        private void FindFriends_Click(object sender, RoutedEventArgs e) => IsDiscoverTabActive = true;

        private void TabStiki_Click(object sender, RoutedEventArgs e) => IsDiscoverTabActive = false;

        private void TabOdkrij_Click(object sender, RoutedEventArgs e) => IsDiscoverTabActive = true;

        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not SuggestedFriend friend) return;

            if (friend.IsAdded)
            {
                friend.IsAdded = false;
                return;
            }

            friend.IsAdded = true;

            var newContact = new Contact
            {
                Nickname = friend.Nickname,
                Status = "Online",
                Conversation = $"{friend.Nickname}: Živjo! 👋\n",
                LastActive = "Pravkar"
            };

            AppData.Contacts.Add(newContact);
            IsDiscoverTabActive = false;
            SelectedContact = newContact;
        }

        private void MenuDark_Click(object sender, RoutedEventArgs e)
        {
            CustomThemeWindow.ApplyDefaultTheme();
            MenuDark.IsChecked = true;
            MenuLight.IsChecked = false;
            MenuCustom.IsChecked = false;
        }

        private void MenuLight_Click(object sender, RoutedEventArgs e)
        {
            CustomThemeWindow.ApplyLightTheme();
            MenuLight.IsChecked = true;
            MenuDark.IsChecked = false;
            MenuCustom.IsChecked = false;
        }

        private void MenuCustom_Click(object sender, RoutedEventArgs e)
        {
            bool wasDark = MenuDark.IsChecked;
            bool wasLight = MenuLight.IsChecked;
            var dlg = new CustomThemeWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                MenuDark.IsChecked = false;
                MenuLight.IsChecked = false;
                MenuCustom.IsChecked = true;
            }
            else
            {
                MenuCustom.IsChecked = false;
                MenuDark.IsChecked = wasDark;
                MenuLight.IsChecked = wasLight;
            }
        }

        private static readonly Random _rnd = new Random();

        private static readonly string[] _autoReplies =
        {
            "Aha, razumem 😊", "Ok, super!", "To je pa res 😄", "Res? Nisem vedel!",
            "Haha, ja 😂", "Lol 😂", "Ma pusti 😄", "Super!", "Idk tbh", "Hmm, mogoče",
            "Ja, čisto res", "Ej, to je pa zanimivo!", "Wow, res?", "Ahahah 😂", "Ok ok",
            "😂😂😂", "No, tako pač je", "Kaj pa jaz vem", "Mhm", "To se pa sliši dobro!",
            "Ej, resno?", "Aww ❤️", "Jaz mislim isto", "Niti slabo!", "Čakaj, res?",
            "Lmao 😂", "Haha točno!", "No, mogoče", "Dobra ideja!", "Tole je pa legit",
            "Absolutno!", "Prav imaš!", "No, okej 😄", "Jaz bi prav tako naredil",
            "Ej, res ne vem 😂", "To je pa huda stvar", "Sem ravno razmišljal o tem",
            "Ja ja, razumem", "Hm, zanimivo", "Kul!", "Ej, to sem že slišal", "Aha, tako je",
            "Sem za!", "Dobro!", "Res imaš prav", "Haha omg", "To je pa malo čudno 😄",
            "Ja, čisto to", "Ni problema!", "Ma da, ne bodi tak", "Okej, to je pa dobro",
            "Hmm, ne vem", "Ej, super ideja!", "Ja, absolutno", "Niti ne", "Haha ja!",
            "Ej, sem za!", "To je pa fino", "Res zanimivo!", "Aha, okej 😊", "Ni slabo!",
            "Res? 😮", "Dobra!", "Jaz isto mislim!", "Haha, point 😂", "Mhm, razumem",
            "Lmaooo 😂", "Aww, to je pa lepo ❤️", "Jaz bi prav isto naredil",
            "Ej, res? Nisem vedel", "Hm, ne vem točno", "To je pa dobro vedeti!",
            "Res? Haha 😂", "Okej okej", "Ej, dej povej več! 👀", "Hm, moram razmisliti",
            "Ja, točno tako", "Lol, res 😂", "Aha, kul!", "Res ni slabo",
            "To se sliši odlično!", "Jaz mislim da ja", "Eh, idk", "Omg, zares? 😮",
            "Haha, najs! 😄", "Ej, to je pa legit", "Ja, sem za to", "Kul, povej mi več",
            "Ej, nisem razmišljal o tem", "Hm, malo čudno, ampak okej 😄",
            "To je pa res dobro!", "Super! 🎉", "Dobra, to mi je všeč!", "Ma, saj vem 😄",
            "Hm, pa kul potem", "No ja, točno", "Ej, hvala ker mi poveš 😊",
            "Lol nima smisla 😂", "Res? Moram preveriti 🤔", "Aja, tak je pa zadeva",
            "Sej vem, ampak kaj ti hočeš 😂", "Ej, kakšen dan imaš pa ti?"
        };

        private static readonly string[] _followUps =
        {
            "Btw, kaj pa ti delaš danes?", "Ej, smo se slišali kdaj res live?",
            "A si videl zadnji ep? 😄", "Btw, ne pozabi na jutri!",
            "Zakaj vprašaš?", "To mi pa daj povej bolj podrobno 👀",
            "Ej, a greš ven ta vikend?", "Btw, sem ravno jedel 😂",
            "In kaj sledi?", "Dej povej!", "Kaj pa misliš ti?",
            "A res? 😮", "Ej, hvala za info!", "No, to je pa zanimivo 😄",
            "Ok, sedaj sem bolj zbuden 😂", "Ej, to moram preveriti",
            "Hm, moram komu povedati 😄", "Btw, ti gre dobro?",
            "Ej, res? To je pa nov info 😄", "Na to nisem pomislil 🤔"
        };

        private string PickAutoResponse(string lower, Random rnd)
        {
            if (lower.Contains("kako si") || lower.Contains("kak si") || lower.Contains("si v redu"))
                return new[] { "Dobro sem, hvala! Ti pa? 😊", "Super! A ti?", "V redu, hvala 😄 In ti?", "Odlično! 😊 Kako pa je pri tebi?" }[rnd.Next(4)];
            if (lower.Contains("hvala"))
                return new[] { "Ni za kaj! 😊", "Z veseljem! 😄", "Anytime! 😊", "Lps! ❤️" }[rnd.Next(4)];
            if (lower.Contains("živjo") || lower.Contains("hej") || lower.Contains("zdravo") || lower.Contains("alo"))
                return new[] { "Ej, živjo! 😊", "Hej hej! 😄", "Živjo! Kaj je novega?", "Aloha! 🌺" }[rnd.Next(4)];
            if (lower.Contains("kdaj"))
                return new[] { "Kmalu, upam! 😄", "Ne vem točno", "Bomo videli 😄", "Kdaj ti paše? 😊" }[rnd.Next(4)];
            if (lower.Contains("kje"))
                return new[] { "Doma sem 😄", "Zdaj sem v šoli 😄", "Ni daleč!", "Ti bom poslal lokacijo 😄" }[rnd.Next(4)];
            if (lower.Contains("?"))
                return new[] { "Hm, nisem prepričan 🤔", "Dobro vprašanje!", "Bom razmislil 😄", "Ne vem, kaj misliš ti?" }[rnd.Next(4)];
            return _autoReplies[rnd.Next(_autoReplies.Length)];
        }

        private void SendBotReply(string userMessage)
        {
            if (SelectedContact == null) return;
            var contact = SelectedContact;
            var rnd = _rnd;

            string reply = PickAutoResponse(userMessage.ToLower(), rnd);
            int delayMs = 700 + rnd.Next(0, 1500);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (SelectedContact != contact) return;
                contact.Conversation += contact.Nickname + ": " + reply + "\n";
                Dispatcher.BeginInvoke(new Action(() => ChatScrollViewer.ScrollToEnd()), DispatcherPriority.Background);

                if (rnd.Next(0, 3) == 0)
                {
                    string followMsg = _followUps[rnd.Next(_followUps.Length)];
                    int followDelayMs = 1500 + rnd.Next(0, 2500);
                    var followTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(followDelayMs) };
                    followTimer.Tick += (s2, e2) =>
                    {
                        followTimer.Stop();
                        if (SelectedContact != contact) return;
                        contact.Conversation += contact.Nickname + ": " + followMsg + "\n";
                        Dispatcher.BeginInvoke(new Action(() => ChatScrollViewer.ScrollToEnd()), DispatcherPriority.Background);
                    };
                    followTimer.Start();
                }
            };
            timer.Start();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e) => ContactSearchText = string.Empty;

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
                        SenderName = line.Trim(),
                        Text = line.Trim(),
                        IsCurrentUser = false
                    });
                    continue;
                }

                string senderName = line.Substring(0, separatorIndex).Trim();
                string text = line.Substring(separatorIndex + 1).Trim();

                CurrentConversationMessages.Add(new ChatMessage
                {
                    SenderName = senderName,
                    Text = text,
                    IsCurrentUser = senderName == AppData.CurrentUser.Nickname
                });
            }
        }

        private void DeleteChatMessage_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedContact == null || sender is not MenuItem menuItem || menuItem.DataContext is not ChatMessage messageToDelete)
            {
                return;
            }

            if (!CurrentConversationMessages.Contains(messageToDelete))
            {
                return;
            }

            CurrentConversationMessages.Remove(messageToDelete);
            RebuildSelectedConversation();
        }

        private void RebuildSelectedConversation()
        {
            if (SelectedContact == null)
            {
                return;
            }

            if (CurrentConversationMessages.Count == 0)
            {
                SelectedContact.Conversation = string.Empty;
                return;
            }

            string rebuiltConversation = string.Empty;

            foreach (ChatMessage message in CurrentConversationMessages)
            {
                string senderName = string.IsNullOrWhiteSpace(message.SenderName)
                    ? (message.IsCurrentUser ? AppData.CurrentUser.Nickname : SelectedContact.Nickname)
                    : message.SenderName;

                rebuiltConversation += senderName + ": " + message.Text + "\n";
            }

            SelectedContact.Conversation = rebuiltConversation;
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

            RebuildContactsView();

            isUpdatingData = false;

            ApplyAutoSaveSettings();
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
