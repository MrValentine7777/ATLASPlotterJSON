using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage? loadedImage;
        private readonly List<AtlasItem> atlasItems = new();
        private Rectangle? selectionRect;
        private Point startPoint;
        private bool isDrawing;
        private string? imagePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    imagePath = openFileDialog.FileName;
                    LoadImage(imagePath);
                    ClearSelections();
                    btnSaveAtlas.IsEnabled = true;
                    btnClearSelections.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadImage(string path)
        {
            loadedImage = new BitmapImage();
            loadedImage.BeginInit();
            loadedImage.CacheOption = BitmapCacheOption.OnLoad;
            loadedImage.UriSource = new Uri(path);
            loadedImage.EndInit();

            displayImage.Source = loadedImage;
            imageCanvas.Width = loadedImage.Width;
            imageCanvas.Height = loadedImage.Height;

            Title = $"Atlas Plotter - {System.IO.Path.GetFileName(path)}";
        }

        private Point SnapToPixel(Point point)
        {
            return new Point(Math.Floor(point.X), Math.Floor(point.Y));
        }

        private void imageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (loadedImage == null) return;

            startPoint = SnapToPixel(e.GetPosition(imageCanvas));
            selectionRect = new Rectangle
            {
                Stroke = Brushes.LimeGreen,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0))
            };

            Canvas.SetLeft(selectionRect, startPoint.X);
            Canvas.SetTop(selectionRect, startPoint.Y);
            imageCanvas.Children.Add(selectionRect);

            isDrawing = true;
            btnAddSelection.IsEnabled = true;
            e.Handled = true;

            UpdateSelectionStatus(startPoint.X, startPoint.Y, 0, 0);
        }

        private void imageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && selectionRect != null)
            {
                var currentPoint = SnapToPixel(e.GetPosition(imageCanvas));

                currentPoint.X = Math.Max(0, Math.Min(currentPoint.X, Math.Floor(imageCanvas.Width)));
                currentPoint.Y = Math.Max(0, Math.Min(currentPoint.Y, Math.Floor(imageCanvas.Height)));

                int left = (int)Math.Min(startPoint.X, currentPoint.X);
                int top = (int)Math.Min(startPoint.Y, currentPoint.Y);
                int width = (int)Math.Abs(currentPoint.X - startPoint.X);
                int height = (int)Math.Abs(currentPoint.Y - startPoint.Y);

                width = Math.Max(1, width);
                height = Math.Max(1, height);

                Canvas.SetLeft(selectionRect, left);
                Canvas.SetTop(selectionRect, top);
                selectionRect.Width = width;
                selectionRect.Height = height;

                UpdateSelectionStatus(left, top, width, height);
            }
        }

        private void imageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
            }
        }

        private void btnAddSelection_Click(object sender, RoutedEventArgs e)
        {
            if (selectionRect == null || string.IsNullOrWhiteSpace(txtSelectionName.Text))
            {
                MessageBox.Show("Please draw a selection and provide a name for it.", "Atlas Plotter",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int left = (int)Canvas.GetLeft(selectionRect);
            int top = (int)Canvas.GetTop(selectionRect);
            int width = (int)selectionRect.Width;
            int height = (int)selectionRect.Height;

            var permRect = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 0, 255))
            };

            Canvas.SetLeft(permRect, left);
            Canvas.SetTop(permRect, top);
            permRect.Width = width;
            permRect.Height = height;

            var textBlock = new TextBlock
            {
                Text = $"{txtSelectionName.Text} [{width}x{height}]",
                Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Foreground = Brushes.Black,
                Padding = new Thickness(2)
            };

            Canvas.SetLeft(textBlock, left);
            Canvas.SetTop(textBlock, top > 20 ? top - 20 : top + height);

            imageCanvas.Children.Remove(selectionRect);
            imageCanvas.Children.Add(permRect);
            imageCanvas.Children.Add(textBlock);

            atlasItems.Add(new AtlasItem
            {
                Name = txtSelectionName.Text,
                X = left,
                Y = top,
                Width = width,
                Height = height
            });

            selectionRect = null;
            txtSelectionName.Text = string.Empty;
            btnAddSelection.IsEnabled = false;

            UpdateSelectionStatus(0, 0, 0, 0, true);
        }

        private void btnSaveAtlas_Click(object sender, RoutedEventArgs e)
        {
            if (loadedImage == null || atlasItems.Count == 0)
            {
                MessageBox.Show("Please load an image and create at least one selection before saving.",
                    "Atlas Plotter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Atlas JSON",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = System.IO.Path.GetFileNameWithoutExtension(imagePath) + "_atlas"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var atlas = new Atlas
                    {
                        ImagePath = Path.GetFileName(imagePath),
                        ImageWidth = loadedImage.PixelWidth,
                        ImageHeight = loadedImage.PixelHeight,
                        Frames = atlasItems
                    };

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string json = JsonSerializer.Serialize(atlas, options);
                    File.WriteAllText(saveFileDialog.FileName, json);

                    MessageBox.Show("Atlas JSON saved successfully!", "Atlas Plotter",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving atlas: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClearSelections_Click(object sender, RoutedEventArgs e)
        {
            ClearSelections();
        }

        private void ClearSelections()
        {
            var children = imageCanvas.Children.Cast<UIElement>().ToList();
            foreach (var child in children)
            {
                if (child != displayImage)
                {
                    imageCanvas.Children.Remove(child);
                }
            }

            atlasItems.Clear();
            selectionRect = null;
            btnAddSelection.IsEnabled = false;
            txtSelectionName.Text = string.Empty;
        }

        private void UpdateSelectionStatus(double x, double y, double width, double height, bool clear = false)
        {
            if (tbSelectionInfo != null)
            {
                if (clear)
                {
                    tbSelectionInfo.Text = "";
                }
                else
                {
                    tbSelectionInfo.Text = $"X: {(int)x}, Y: {(int)y}, Width: {(int)width}, Height: {(int)height}";
                }
            }
        }
    }

    public class AtlasItem
    {
        public string Name { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Atlas
    {
        public string ImagePath { get; set; } = string.Empty;
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public List<AtlasItem> Frames { get; set; } = new List<AtlasItem>();
    }
}