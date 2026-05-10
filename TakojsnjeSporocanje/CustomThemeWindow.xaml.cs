using System;
using System.Windows;
using System.Windows.Media;

namespace TakojsnjeSporocanje
{
    public partial class CustomThemeWindow : Window
    {
        private Color _bgColor = Color.FromRgb(0x0D, 0x0F, 0x1A);
        private Color _accentColor = Color.FromRgb(0x63, 0x66, 0xF1);
        private Color _textColor = Color.FromRgb(0xE2, 0xE8, 0xF0);

        public CustomThemeWindow()
        {
            InitializeComponent();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var bg = _bgColor;
            var accent = _accentColor;
            var text = _textColor;

            BgSwatch.Background = new SolidColorBrush(bg);
            BgHexText.Text = ColorToHex(bg);
            AccentSwatch.Background = new SolidColorBrush(accent);
            AccentHexText.Text = ColorToHex(accent);
            TextSwatch.Background = new SolidColorBrush(text);
            TextHexText.Text = ColorToHex(text);

            var sidebar = Lighten(bg, 0.06f);
            var secondary = Lighten(bg, 0.14f);
            var tertiary = Lighten(bg, 0.20f);

            PvSidebar.Background = new SolidColorBrush(sidebar);
            PvChatArea.Background = new SolidColorBrush(Lighten(bg, 0.09f));

            PvUserCard.Background = new SolidColorBrush(secondary);
            PvUserAvatar.Background = new SolidColorBrush(bg);
            PvUserName.Foreground = new SolidColorBrush(text);

            PvContact1.Background = new SolidColorBrush(secondary);
            PvContact1Av.Background = new SolidColorBrush(tertiary);
            PvContact1Txt.Foreground = new SolidColorBrush(text);

            PvContact2.Background = new SolidColorBrush(secondary);
            PvContact2Av.Background = new SolidColorBrush(tertiary);
            PvContact2Txt.Foreground = new SolidColorBrush(text);

            PvBubbleIn.Background = new SolidColorBrush(secondary);
            PvBubbleInTxt.Foreground = new SolidColorBrush(text);
            PvBubbleOut.Background = new LinearGradientBrush(
                Lighten(accent, -0.1f), accent, new Point(0, 0), new Point(1, 1));
        }

        private void PickBg_Click(object sender, RoutedEventArgs e)
        {
            if (PickColor(_bgColor, out var chosen))
            {
                _bgColor = chosen;
                UpdatePreview();
            }
        }

        private void PickAccent_Click(object sender, RoutedEventArgs e)
        {
            if (PickColor(_accentColor, out var chosen))
            {
                _accentColor = chosen;
                UpdatePreview();
            }
        }

        private void PickText_Click(object sender, RoutedEventArgs e)
        {
            if (PickColor(_textColor, out var chosen))
            {
                _textColor = chosen;
                UpdatePreview();
            }
        }

        private static bool PickColor(Color current, out Color result)
        {
            var dlg = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B),
                FullOpen = true
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                result = Color.FromRgb(dlg.Color.R, dlg.Color.G, dlg.Color.B);
                return true;
            }
            result = current;
            return false;
        }

        private void PresetOcean_Click(object sender, RoutedEventArgs e)
        {
            _bgColor = Color.FromRgb(0x0A, 0x16, 0x28);
            _accentColor = Color.FromRgb(0x0E, 0xA5, 0xE9);
            _textColor = Color.FromRgb(0xE0, 0xF2, 0xFE);
            UpdatePreview();
        }

        private void PresetGrape_Click(object sender, RoutedEventArgs e)
        {
            _bgColor = Color.FromRgb(0x17, 0x0B, 0x2E);
            _accentColor = Color.FromRgb(0xA8, 0x55, 0xF7);
            _textColor = Color.FromRgb(0xF3, 0xE8, 0xFF);
            UpdatePreview();
        }

        private void PresetMocha_Click(object sender, RoutedEventArgs e)
        {
            _bgColor = Color.FromRgb(0x1A, 0x0F, 0x0A);
            _accentColor = Color.FromRgb(0xD9, 0x77, 0x06);
            _textColor = Color.FromRgb(0xFE, 0xF3, 0xC7);
            UpdatePreview();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(_bgColor, _accentColor, _textColor);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public static void ApplyTheme(Color bg, Color accent, Color text)
        {
            var res = Application.Current.Resources;
            SetBrush(res, "BrBgDeep", bg);
            SetBrush(res, "BrBgSidebar", Lighten(bg, 0.06f));
            SetBrush(res, "BrBgPrimary", Lighten(bg, 0.09f));
            SetBrush(res, "BrBgSecondary", Lighten(bg, 0.14f));
            SetBrush(res, "BrBgTertiary", Lighten(bg, 0.20f));
            SetBrush(res, "BrAccent", accent);
            SetBrush(res, "BrAccentSoft", Lighten(accent, 0.1f));
            SetBrush(res, "BrTextPrimary", text);
            SetBrush(res, "BrTextSecondary", WithAlpha(text, 0.70f));
            SetBrush(res, "BrTextMuted", WithAlpha(text, 0.40f));
            SetBrush(res, "BrBorderGlow", WithAlpha(accent, 0.31f));
            res["BrAccentGradient"] = new LinearGradientBrush(
                Lighten(accent, -0.1f), accent,
                new Point(0, 0), new Point(1, 1));
        }

        public static void ApplyDefaultTheme()
        {
            ApplyTheme(
                Color.FromRgb(0x0D, 0x0F, 0x1A),
                Color.FromRgb(0x63, 0x66, 0xF1),
                Color.FromRgb(0xE2, 0xE8, 0xF0));
        }

        public static void ApplyLightTheme()
        {
            var res = Application.Current.Resources;
            SetBrush(res, "BrBgDeep",        Color.FromRgb(0xF1, 0xF5, 0xF9));
            SetBrush(res, "BrBgSidebar",     Color.FromRgb(0xE2, 0xE8, 0xF0));
            SetBrush(res, "BrBgPrimary",     Color.FromRgb(0xF8, 0xFA, 0xFC));
            SetBrush(res, "BrBgSecondary",   Color.FromRgb(0xCB, 0xD5, 0xE1));
            SetBrush(res, "BrBgTertiary",    Color.FromRgb(0xB0, 0xBE, 0xCC));
            SetBrush(res, "BrAccent",        Color.FromRgb(0x4F, 0x46, 0xE5));
            SetBrush(res, "BrAccentSoft",    Color.FromRgb(0x63, 0x66, 0xF1));
            SetBrush(res, "BrTextPrimary",   Color.FromRgb(0x1E, 0x29, 0x3B));
            SetBrush(res, "BrTextSecondary", Color.FromRgb(0x47, 0x55, 0x69));
            SetBrush(res, "BrTextMuted",     Color.FromRgb(0x64, 0x74, 0x8B));
            SetBrush(res, "BrBorderGlow",    Color.FromArgb(0x50, 0x4F, 0x46, 0xE5));
            res["BrAccentGradient"] = new LinearGradientBrush(
                Color.FromRgb(0x4F, 0x46, 0xE5), Color.FromRgb(0x63, 0x66, 0xF1),
                new Point(0, 0), new Point(1, 1));
        }

        private static void SetBrush(ResourceDictionary res, string key, Color color)
        {
            res[key] = new SolidColorBrush(color);
        }

        private static Color Lighten(Color c, float amount)
        {
            return Color.FromRgb(
                (byte)Math.Clamp(c.R + (int)(amount * 255), 0, 255),
                (byte)Math.Clamp(c.G + (int)(amount * 255), 0, 255),
                (byte)Math.Clamp(c.B + (int)(amount * 255), 0, 255));
        }

        private static Color WithAlpha(Color c, float alpha)
        {
            return Color.FromArgb((byte)(alpha * 255), c.R, c.G, c.B);
        }

        private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
