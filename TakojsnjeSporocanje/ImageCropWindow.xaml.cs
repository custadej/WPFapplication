using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TakojsnjeSporocanje
{
    public partial class ImageCropWindow : Window, INotifyPropertyChanged
    {
        private readonly BitmapImage originalBitmap;
        private bool isDragging;
        private Point dragOffset;
        private Rect displayedImageBounds;
        private BitmapSource previewImage;

        public BitmapSource PreviewImage
        {
            get => previewImage;
            set
            {
                previewImage = value;
                OnPropertyChanged();
            }
        }

        public string CroppedImagePath { get; private set; } = string.Empty;

        public ImageCropWindow(string imagePath)
        {
            InitializeComponent();
            DataContext = this;

            originalBitmap = new BitmapImage();
            originalBitmap.BeginInit();
            originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
            originalBitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            originalBitmap.EndInit();
            originalBitmap.Freeze();

            SourceImage.Source = originalBitmap;
            Loaded += (_, _) => UpdateDisplayedImageBounds(true);
        }

        private void ImageViewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDisplayedImageBounds(false);
        }

        private void SelectionEllipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            Point mousePosition = e.GetPosition(OverlayCanvas);
            dragOffset = new Point(mousePosition.X - Canvas.GetLeft(SelectionEllipse), mousePosition.Y - Canvas.GetTop(SelectionEllipse));
            SelectionEllipse.CaptureMouse();
        }

        private void SelectionEllipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                return;
            }

            Point mousePosition = e.GetPosition(OverlayCanvas);
            UpdateEllipsePosition(mousePosition.X - dragOffset.X, mousePosition.Y - dragOffset.Y);
            UpdatePreview();
        }

        private void SelectionEllipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            SelectionEllipse.ReleaseMouseCapture();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || displayedImageBounds.Width <= 0 || displayedImageBounds.Height <= 0)
            {
                return;
            }

            double size = Math.Min(e.NewValue, Math.Min(displayedImageBounds.Width, displayedImageBounds.Height));
            double centerX = Canvas.GetLeft(SelectionEllipse) + (SelectionEllipse.Width / 2);
            double centerY = Canvas.GetTop(SelectionEllipse) + (SelectionEllipse.Height / 2);

            SelectionEllipse.Width = size;
            SelectionEllipse.Height = size;

            UpdateEllipsePosition(centerX - (size / 2), centerY - (size / 2));
            UpdatePreview();
        }

        private void ResetSelection_Click(object sender, RoutedEventArgs e)
        {
            ResetSelection();
        }

        private void UpdateDisplayedImageBounds(bool resetSelection)
        {
            if (ImageViewport.ActualWidth <= 0 || ImageViewport.ActualHeight <= 0)
            {
                return;
            }

            double scale = Math.Min(ImageViewport.ActualWidth / originalBitmap.PixelWidth, ImageViewport.ActualHeight / originalBitmap.PixelHeight);
            double width = originalBitmap.PixelWidth * scale;
            double height = originalBitmap.PixelHeight * scale;
            double left = (ImageViewport.ActualWidth - width) / 2;
            double top = (ImageViewport.ActualHeight - height) / 2;

            displayedImageBounds = new Rect(left, top, width, height);
            OverlayCanvas.Width = ImageViewport.ActualWidth;
            OverlayCanvas.Height = ImageViewport.ActualHeight;

            if (resetSelection || SelectionEllipse.Width == 0)
            {
                ResetSelection();
                return;
            }

            double maxSize = Math.Min(displayedImageBounds.Width, displayedImageBounds.Height);
            SizeSlider.Maximum = Math.Max(120, maxSize);
            if (SelectionEllipse.Width > maxSize)
            {
                SelectionEllipse.Width = maxSize;
                SelectionEllipse.Height = maxSize;
            }

            UpdateEllipsePosition(Canvas.GetLeft(SelectionEllipse), Canvas.GetTop(SelectionEllipse));
            UpdatePreview();
        }

        private void ResetSelection()
        {
            if (displayedImageBounds.Width <= 0 || displayedImageBounds.Height <= 0)
            {
                return;
            }

            double defaultSize = Math.Min(displayedImageBounds.Width, displayedImageBounds.Height) * 0.8;
            defaultSize = Math.Max(120, Math.Min(defaultSize, Math.Min(displayedImageBounds.Width, displayedImageBounds.Height)));

            SizeSlider.Maximum = Math.Max(140, Math.Min(displayedImageBounds.Width, displayedImageBounds.Height));
            SizeSlider.Value = defaultSize;

            SelectionEllipse.Width = defaultSize;
            SelectionEllipse.Height = defaultSize;

            double left = displayedImageBounds.Left + ((displayedImageBounds.Width - defaultSize) / 2);
            double top = displayedImageBounds.Top + ((displayedImageBounds.Height - defaultSize) / 2);

            UpdateEllipsePosition(left, top);
            UpdatePreview();
        }

        private void UpdateEllipsePosition(double left, double top)
        {
            left = Math.Max(displayedImageBounds.Left, Math.Min(left, displayedImageBounds.Right - SelectionEllipse.Width));
            top = Math.Max(displayedImageBounds.Top, Math.Min(top, displayedImageBounds.Bottom - SelectionEllipse.Height));

            Canvas.SetLeft(SelectionEllipse, left);
            Canvas.SetTop(SelectionEllipse, top);
        }

        private void UpdatePreview()
        {
            if (displayedImageBounds.Width <= 0 || displayedImageBounds.Height <= 0 || SelectionEllipse.Width <= 0)
            {
                return;
            }

            double scaleX = originalBitmap.PixelWidth / displayedImageBounds.Width;
            double scaleY = originalBitmap.PixelHeight / displayedImageBounds.Height;

            int x = (int)Math.Round((Canvas.GetLeft(SelectionEllipse) - displayedImageBounds.Left) * scaleX);
            int y = (int)Math.Round((Canvas.GetTop(SelectionEllipse) - displayedImageBounds.Top) * scaleY);
            int size = (int)Math.Round(SelectionEllipse.Width * scaleX);

            x = Math.Max(0, Math.Min(x, originalBitmap.PixelWidth - size));
            y = Math.Max(0, Math.Min(y, originalBitmap.PixelHeight - size));
            size = Math.Max(1, Math.Min(size, Math.Min(originalBitmap.PixelWidth - x, originalBitmap.PixelHeight - y)));

            PreviewImage = new CroppedBitmap(originalBitmap, new Int32Rect(x, y, size, size));
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TakojsnjeSporocanje",
                "ProfileImages");

            Directory.CreateDirectory(outputDirectory);

            string outputPath = Path.Combine(outputDirectory, $"profile_{Guid.NewGuid():N}.png");

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(PreviewImage));

            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(stream);
            }

            CroppedImagePath = outputPath;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
