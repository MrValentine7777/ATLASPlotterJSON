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
        private string? imagePath;
        private Dictionary<int, SpriteItemMarker> spriteMarkers = new Dictionary<int, SpriteItemMarker>();

        // Pixel location tracking
        private PixelLocationDisplay? pixelLocationDisplay;
        private bool isTrackingPixel = false;
        private Point currentPixelLocation;

        // Zoom related properties
        private double currentZoom = 1.0;
        private const double ZoomIncrement = 0.25;
        private const double MinZoom = 0.25;
        private const double MaxZoom = 8.0;

        // Make currentZoom public so displays can access it
        public double CurrentZoom => currentZoom;

        public MainWindow()
        {
            InitializeComponent();
            UpdateZoomDisplay();

            // Subscribe to sprite collection events
            jsonDataEntry.SelectedSpriteChanged += OnSelectedSpriteChanged;
            jsonDataEntry.SpriteAdded += OnSpriteAdded;
            jsonDataEntry.SpriteRemoved += OnSpriteRemoved;
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
                    ResetZoom();

                    // Initialize pixel location display after image is loaded
                    InitializePixelLocationDisplay();

                    // Create markers for any existing sprite items
                    CreateMarkersForExistingSprites();
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

        private void InitializePixelLocationDisplay()
        {
            // Remove existing display if any
            if (pixelLocationDisplay != null)
            {
                imageCanvas.Children.Remove(pixelLocationDisplay);
            }

            // Create new pixel location display
            pixelLocationDisplay = new PixelLocationDisplay(this);
            imageCanvas.Children.Add(pixelLocationDisplay);
            pixelLocationDisplay.Hide(); // Initially hidden
        }

        private void CreateMarkersForExistingSprites()
        {
            // Clear existing markers first
            ClearSpriteMarkers();

            // Create markers for each sprite in the collection
            foreach (var sprite in jsonDataEntry.SpriteCollection.Items)
            {
                CreateSpriteMarker(sprite);
            }
        }

        private void ClearSpriteMarkers()
        {
            foreach (var marker in spriteMarkers.Values)
            {
                imageCanvas.Children.Remove(marker);
            }
            spriteMarkers.Clear();
        }

        private void CreateSpriteMarker(SpriteItem sprite)
        {
            if (loadedImage == null) return;

            // Get the color for this sprite
            Color markerColor = jsonDataEntry.SpriteCollection.GetItemColor(sprite.Id);

            // Create a new marker
            var marker = new SpriteItemMarker(sprite, markerColor);
            marker.MarkerSelected += Marker_Selected;

            // Add it to the canvas and dictionary
            imageCanvas.Children.Add(marker);
            spriteMarkers[sprite.Id] = marker;

            // Update its appearance based on whether it's selected
            marker.UpdateAppearance(sprite == jsonDataEntry.SpriteCollection.SelectedItem);

            // Update marker position with current zoom
            marker.UpdatePosition(currentZoom);
        }

        private void Marker_Selected(object sender, SpriteItem sprite)
        {
            // Update the selected sprite in the collection
            jsonDataEntry.SpriteCollection.SelectedItem = sprite;
        }

        private void OnSelectedSpriteChanged(object sender, SpriteItem sprite)
        {
            // Update the appearance of all markers
            foreach (var marker in spriteMarkers.Values)
            {
                marker.UpdateAppearance(marker.SpriteItem == sprite);
            }
        }

        private void OnSpriteAdded(object sender, SpriteItem sprite)
        {
            if (loadedImage != null)
            {
                CreateSpriteMarker(sprite);
            }
        }

        private void OnSpriteRemoved(object sender, SpriteItem sprite)
        {
            if (spriteMarkers.TryGetValue(sprite.Id, out var marker))
            {
                imageCanvas.Children.Remove(marker);
                spriteMarkers.Remove(sprite.Id);
            }
        }

        public Point SnapToPixel(Point point)
        {
            // Convert mouse position from transformed canvas coordinates to original coordinates
            point.X /= currentZoom;
            point.Y /= currentZoom;
            return new Point(Math.Floor(point.X), Math.Floor(point.Y));
        }

        private void imageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (loadedImage == null) return;

            currentPixelLocation = SnapToPixel(e.GetPosition(imageCanvas));

            // Begin pixel tracking
            isTrackingPixel = true;

            // Show and update pixel location display
            if (pixelLocationDisplay != null)
            {
                pixelLocationDisplay.UpdatePosition(currentPixelLocation, currentZoom);
                pixelLocationDisplay.Show();

                // Update JSON data source with current location
                jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                // Update the marker position for the selected sprite
                UpdateSelectedSpriteMarker();
            }

            // Update status display
            UpdateSelectionStatus(currentPixelLocation.X, currentPixelLocation.Y, 0, 0);

            e.Handled = true;
        }

        private void imageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (loadedImage == null) return;

            Point mousePosition = SnapToPixel(e.GetPosition(imageCanvas));

            // Always update status bar with mouse position
            UpdateSelectionStatus(mousePosition.X, mousePosition.Y, 0, 0);

            if (isTrackingPixel)
            {
                currentPixelLocation = mousePosition;

                // Keep pixel location within image bounds
                currentPixelLocation.X = Math.Max(0, Math.Min(currentPixelLocation.X, imageCanvas.Width / currentZoom - 1));
                currentPixelLocation.Y = Math.Max(0, Math.Min(currentPixelLocation.Y, imageCanvas.Height / currentZoom - 1));

                // Update pixel location display
                if (pixelLocationDisplay != null)
                {
                    pixelLocationDisplay.UpdatePosition(currentPixelLocation, currentZoom);

                    // Update JSON data with current location
                    jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                    // Update the marker position for the selected sprite
                    UpdateSelectedSpriteMarker();
                }
            }
        }

        private void UpdateSelectedSpriteMarker()
        {
            var selectedSprite = jsonDataEntry.SpriteCollection.SelectedItem;
            if (selectedSprite != null && spriteMarkers.TryGetValue(selectedSprite.Id, out var marker))
            {
                marker.UpdatePosition(currentZoom);
            }
        }

        private void imageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isTrackingPixel)
            {
                isTrackingPixel = false;

                // Keep pixel location display visible
                if (pixelLocationDisplay != null)
                {
                    pixelLocationDisplay.Show();
                }

                // Update JSON data with final location
                jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                // Update the marker position for the selected sprite
                UpdateSelectedSpriteMarker();
            }
        }

        public void UpdateHandlePositions()
        {
            // Method kept for compatibility
        }

        private void btnAddSelection_Click(object sender, RoutedEventArgs e)
        {
            // Not needed with the new approach
        }

        private void btnSaveAtlas_Click(object sender, RoutedEventArgs e)
        {
            if (loadedImage == null)
            {
                MessageBox.Show("Please load an image before saving.",
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
                    // Create a simple object to save - save the entire collection
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string json = JsonSerializer.Serialize(jsonDataEntry.SpriteCollection, options);
                    File.WriteAllText(saveFileDialog.FileName, json);

                    MessageBox.Show($"JSON data saved successfully with {jsonDataEntry.SpriteCollection.Items.Count} sprite items!",
                        "Atlas Plotter", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving JSON: {ex.Message}", "Error",
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
            // Remove all elements except the image and pixel location display
            var children = imageCanvas.Children.Cast<UIElement>().ToList();
            foreach (var child in children)
            {
                if (child != displayImage && child != pixelLocationDisplay && !(child is SpriteItemMarker))
                {
                    imageCanvas.Children.Remove(child);
                }
            }

            // Clear sprite markers
            ClearSpriteMarkers();

            atlasItems.Clear();

            // Hide pixel location display
            if (pixelLocationDisplay != null)
            {
                pixelLocationDisplay.Hide();
            }
        }

        public void UpdateSelectionStatus(double x, double y, double width, double height, bool clear = false)
        {
            if (tbSelectionInfo != null)
            {
                if (clear)
                {
                    tbSelectionInfo.Text = "";
                }
                else
                {
                    tbSelectionInfo.Text = $"X: {(int)x}, Y: {(int)y}";
                }
            }
        }

        // Zoom functionality
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ApplyZoom(currentZoom + ZoomIncrement);
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ApplyZoom(currentZoom - ZoomIncrement);
        }

        private void btnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        private void ResetZoom()
        {
            ApplyZoom(1.0);
        }

        private void ApplyZoom(double zoom)
        {
            // Clamp zoom level to limits
            zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));

            // Apply zoom
            currentZoom = zoom;
            zoomTransform.ScaleX = zoom;
            zoomTransform.ScaleY = zoom;

            // Update UI to show current zoom level
            UpdateZoomDisplay();

            // Update pixel location display if visible
            if (pixelLocationDisplay != null && pixelLocationDisplay.Visibility == Visibility.Visible)
            {
                pixelLocationDisplay.UpdatePosition(pixelLocationDisplay.CurrentLocation, currentZoom);
            }

            // Update all sprite markers with new zoom
            foreach (var marker in spriteMarkers.Values)
            {
                marker.UpdatePosition(currentZoom);
            }
        }

        private void UpdateZoomDisplay()
        {
            txtZoomLevel.Text = $"{currentZoom * 100:0}%";
        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Calculate zoom factor based on wheel direction
                double zoomFactor = e.Delta > 0 ? ZoomIncrement : -ZoomIncrement;
                double newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, currentZoom + zoomFactor));

                // Apply the zoom
                ApplyZoom(newZoom);

                // Prevent the ScrollViewer from scrolling
                e.Handled = true;
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