using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TakojsnjeSporocanje
{
    public partial class MessageComposerControl : UserControl
    {
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register(nameof(MessageText), typeof(string), typeof(MessageComposerControl), new PropertyMetadata(string.Empty));

        public static readonly RoutedEvent SendMessageRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(SendMessageRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MessageComposerControl));

        public MessageComposerControl()
        {
            InitializeComponent();
        }

        private bool isNormalizingText;

        public string MessageText
        {
            get => (string)GetValue(MessageTextProperty);
            set => SetValue(MessageTextProperty, value);
        }

        public event RoutedEventHandler SendMessageRequested
        {
            add => AddHandler(SendMessageRequestedEvent, value);
            remove => RemoveHandler(SendMessageRequestedEvent, value);
        }

        public void ClearMessage()
        {
            MessageText = string.Empty;
        }

        public void FocusInput()
        {
            ComposerTextBox.Focus();
            ComposerTextBox.CaretIndex = ComposerTextBox.Text.Length;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SendMessageRequestedEvent));
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            MessageText += " :)";
            FocusInput();
        }

        private void ComposerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                RaiseEvent(new RoutedEventArgs(SendMessageRequestedEvent));
            }
        }

        private void ComposerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isNormalizingText)
            {
                return;
            }

            string normalizedText = NormalizeLongLines(ComposerTextBox.Text);
            if (normalizedText == ComposerTextBox.Text)
            {
                return;
            }

            int caretIndex = ComposerTextBox.CaretIndex;

            isNormalizingText = true;
            ComposerTextBox.Text = normalizedText;
            MessageText = normalizedText;
            ComposerTextBox.CaretIndex = normalizedText.Length < caretIndex ? normalizedText.Length : caretIndex;
            isNormalizingText = false;
        }

        private string NormalizeLongLines(string text)
        {
            if (string.IsNullOrEmpty(text) || ComposerTextBox.ActualWidth <= 0)
            {
                return text;
            }

            double availableWidth = ComposerTextBox.ActualWidth - 8;
            if (availableWidth <= 0)
            {
                return text;
            }

            Typeface typeface = new Typeface(
                ComposerTextBox.FontFamily,
                ComposerTextBox.FontStyle,
                ComposerTextBox.FontWeight,
                ComposerTextBox.FontStretch);

            string[] existingLines = text.Replace("\r\n", "\n").Split('\n');
            string result = string.Empty;

            for (int lineIndex = 0; lineIndex < existingLines.Length; lineIndex++)
            {
                string sourceLine = existingLines[lineIndex];
                string currentLine = string.Empty;

                foreach (char character in sourceLine)
                {
                    string candidate = currentLine + character;
                    FormattedText formattedText = new FormattedText(
                        candidate,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        ComposerTextBox.FontSize,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    if (formattedText.Width > availableWidth && currentLine.Length > 0)
                    {
                        result += currentLine + "\n";
                        currentLine = character.ToString();
                    }
                    else
                    {
                        currentLine = candidate;
                    }
                }

                result += currentLine;

                if (lineIndex < existingLines.Length - 1)
                {
                    result += "\n";
                }
            }

            return result;
        }
    }
}
