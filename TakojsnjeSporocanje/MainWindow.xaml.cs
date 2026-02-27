using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace TakojsnjeSporocanje
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> conversations = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            if (ContactsListBox.Items.Count > 0)
                ContactsListBox.SelectedIndex = 0;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuAddContact_Click(object sender, RoutedEventArgs e)
        {
            InputDialog dialog = new InputDialog("Vnesite ime novega stika:", "NovStik");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    ContactsListBox.Items.Add(new ListBoxItem { Content = dialog.ResponseText });
                }
            }
        }

        private void MenuRemoveContact_Click(object sender, RoutedEventArgs e)
        {
            if (ContactsListBox.SelectedItem != null)
                ContactsListBox.Items.Remove(ContactsListBox.SelectedItem);
        }

        private void MenuEditContact_Click(object sender, RoutedEventArgs e)
        {
            if (ContactsListBox.SelectedItem == null) return;

            ListBoxItem item = (ListBoxItem)ContactsListBox.SelectedItem;

            InputDialog dialog = new InputDialog("Novo ime stika:", item.Content.ToString());
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    item.Content = dialog.ResponseText;
                }
            }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nastavitve bodo implementirane kasneje.");
        }

        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContactsListBox.SelectedItem == null) return;

            ListBoxItem item = (ListBoxItem)ContactsListBox.SelectedItem;
            string name = item.Content.ToString();

            ChatTitleTextBlock.Text = "Pogovor z: " + name;

            if (!conversations.ContainsKey(name))
                conversations[name] = name + ": Živjo!\n";

            ChatContentTextBlock.Text = conversations[name];
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (ContactsListBox.SelectedItem == null) return;
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text)) return;

            ListBoxItem item = (ListBoxItem)ContactsListBox.SelectedItem;
            string contactName = item.Content.ToString();
            string nickname = NicknameTextBox.Text;
            string message = MessageTextBox.Text;

            if (!conversations.ContainsKey(contactName))
                conversations[contactName] = "";

            conversations[contactName] += nickname + ": " + message + "\n";

            string response;

            if (message.ToLower().Contains("kako"))
                response = "Super sem 😄";
            else if (message.ToLower().Contains("ime"))
                response = "Lepo ime imaš 😉";
            else
                response = "OK 👍";

            conversations[contactName] += contactName + ": " + response + "\n";

            ChatContentTextBlock.Text = conversations[contactName];

            MessageTextBox.Clear();
            ChatScrollViewer.ScrollToEnd();
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text += "😊";
        }
    }
}