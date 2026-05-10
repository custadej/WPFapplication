using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TakojsnjeSporocanje
{
    public class SuggestedFriend : INotifyPropertyChanged
    {
        public string Nickname { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string MutualInfo { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public Color AvatarColor { get; set; }

        public SolidColorBrush AvatarBrush => new SolidColorBrush(AvatarColor);

        private bool isAdded;
        public bool IsAdded
        {
            get => isAdded;
            set { isAdded = value; OnPropertyChanged(); OnPropertyChanged(nameof(ButtonText)); }
        }

        public string ButtonText => IsAdded ? "Dodano ✓" : "Dodaj";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
