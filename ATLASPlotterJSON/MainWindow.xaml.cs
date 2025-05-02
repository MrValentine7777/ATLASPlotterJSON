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
    /// This is the main entry point of the ATLAS Plotter application.
    /// 
    /// APPLICATION OVERVIEW:
    /// ATLAS Plotter is a tool for defining sprite regions within a sprite atlas image.
    /// A sprite atlas is a single image containing multiple individual sprites/graphics.
    /// This tool allows users to:
    /// 1. Load a sprite atlas image
    /// 2. Select and define individual sprites within the atlas
    /// 3. Configure sprite properties (position, size, colliders, etc.)
    /// 4. Save the sprite data as JSON for use in games or other applications
    /// 
    /// ARCHITECTURAL OVERVIEW:
    /// - MainWindow (this class): The UI container and main controller
    /// - SpriteItemCollection: Manages the collection of sprites and their data
    /// - SpriteItemMarker: Visual representation of sprites on the canvas
    /// - PixelLocationDisplay: Shows precise pixel coordinates when placing sprites
    /// - JsonDataEntryControl: UI for editing sprite properties
    /// - JsonDataModels: Data structures for sprite information
    /// </summary>
    public partial class MainWindow : Window
    {
        // CORE IMAGE AND DATA PROPERTIES
        /// <summary>The loaded sprite atlas image</summary>
        private BitmapImage? loadedImage;
        
        /// <summary>Legacy collection for atlas items (mostly replaced by SpriteCollection)</summary>
        private readonly List<AtlasItem> atlasItems = new();
        
        /// <summary>Path to the currently loaded image file</summary>
        private string? imagePath;
        
        /// <summary>
        /// Dictionary mapping sprite IDs to their visual markers on the canvas
        /// This allows quick lookup of markers when sprites need to be updated
        /// </summary>
        private Dictionary<int, SpriteItemMarker> spriteMarkers = new Dictionary<int, SpriteItemMarker>();

        // PIXEL LOCATION TRACKING PROPERTIES
        /// <summary>
        /// Visual display showing the current pixel location
        /// Created by InitializePixelLocationDisplay() when an image is loaded
        /// </summary>
        private PixelLocationDisplay? pixelLocationDisplay;
        
        /// <summary>True when actively tracking mouse movement for pixel selection</summary>
        private bool isTrackingPixel = false;
        
        /// <summary>The current pixel location being tracked</summary>
        private Point currentPixelLocation;

        // ZOOM RELATED PROPERTIES
        /// <summary>Current zoom level of the canvas (1.0 = 100%)</summary>
        private double currentZoom = 1.0;
        
        /// <summary>How much to change zoom with each zoom operation (25%)</summary>
        private const double ZoomIncrement = 0.25;
        
        /// <summary>Minimum allowed zoom level (25%)</summary>
        private const double MinZoom = 0.25;
        
        /// <summary>Maximum allowed zoom level (800%)</summary>
        private const double MaxZoom = 8.0;

        /// <summary>
        /// Public accessor for current zoom level
        /// Other components like PixelLocationDisplay and SpriteItemMarker
        /// use this to adjust their visual appearance at different zoom levels
        /// </summary>
        public double CurrentZoom => currentZoom;

        /// <summary>
        /// Initialize the main application window
        /// </summary>
        public MainWindow()
        {
            // Initialize the WPF components defined in XAML
            InitializeComponent();
            
            // Set initial zoom display text
            UpdateZoomDisplay();

            // COMPONENT CONNECTION:
            // Connect to JsonDataEntryControl events to stay synchronized
            // These events notify us when sprites are added, removed, or selected
            jsonDataEntry.SelectedSpriteChanged += OnSelectedSpriteChanged;
            jsonDataEntry.SpriteAdded += OnSpriteAdded;
            jsonDataEntry.SpriteRemoved += OnSpriteRemoved;
        }

        /// <summary>
        /// Handles the "Load Image" button click
        /// Displays file picker and loads the selected sprite atlas image
        /// </summary>
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            // Create and configure a file picker dialog for images
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*"
            };

            // Show the dialog and process the result if user selected a file
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Store the selected image path
                    imagePath = openFileDialog.FileName;
                    
                    // APPLICATION FLOW:
                    // 1. Load the image into the canvas
                    LoadImage(imagePath);
                    
                    // 2. Clear any existing sprite selections
                    ClearSelections();
                    
                    // 3. Enable the UI buttons for saving and clearing
                    btnSaveAtlas.IsEnabled = true;
                    btnClearSelections.IsEnabled = true;
                    
                    // 4. Reset zoom to 100%
                    ResetZoom();

                    // 5. Create the pixel location tracker for precise positioning
                    InitializePixelLocationDisplay();

                    // 6. Create visual markers for any sprites already in the collection
                    CreateMarkersForExistingSprites();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Loads an image from the specified path into the image canvas
        /// </summary>
        /// <param name="path">Path to the image file to load</param>
        private void LoadImage(string path)
        {
            // Create a new bitmap image and configure loading options
            loadedImage = new BitmapImage();
            loadedImage.BeginInit();
            loadedImage.CacheOption = BitmapCacheOption.OnLoad; // Load entire image into memory at once
            loadedImage.UriSource = new Uri(path);
            loadedImage.EndInit();

            // COMPONENT CONNECTION:
            // Apply the loaded image to the Image control in the XAML
            displayImage.Source = loadedImage;
            
            // Size the canvas to match the image dimensions
            // This ensures the canvas is exactly the size of the sprite atlas
            imageCanvas.Width = loadedImage.Width;
            imageCanvas.Height = loadedImage.Height;

            // Update window title to show which image is loaded
            Title = $"Atlas Plotter - {System.IO.Path.GetFileName(path)}";
        }

        /// <summary>
        /// Creates or recreates the pixel location display after loading an image
        /// The PixelLocationDisplay shows a red highlight box and coordinates
        /// when the user is selecting a position on the image
        /// </summary>
        private void InitializePixelLocationDisplay()
        {
            // Remove any existing display first
            if (pixelLocationDisplay != null)
            {
                imageCanvas.Children.Remove(pixelLocationDisplay);
            }

            // COMPONENT CONNECTION:
            // Create a new PixelLocationDisplay linked to this window
            // The display will show precise pixel coordinates when placing sprites
            pixelLocationDisplay = new PixelLocationDisplay(this);
            imageCanvas.Children.Add(pixelLocationDisplay);
            pixelLocationDisplay.Hide(); // Initially hidden until user clicks
        }

        /// <summary>
        /// Creates visual markers for all sprites in the collection
        /// Called when an image is loaded to display existing sprite data
        /// </summary>
        private void CreateMarkersForExistingSprites()
        {
            // Remove any existing markers first
            ClearSpriteMarkers();

            // COMPONENT CONNECTION:
            // For each sprite in the JsonDataEntryControl's collection,
            // create a visual marker to show its position on the sprite atlas
            foreach (var sprite in jsonDataEntry.SpriteCollection.Items)
            {
                CreateSpriteMarker(sprite);
            }
        }

        /// <summary>
        /// Removes all sprite markers from the canvas and clears the markers dictionary
        /// </summary>
        private void ClearSpriteMarkers()
        {
            // Remove each marker from the visual canvas
            foreach (var marker in spriteMarkers.Values)
            {
                imageCanvas.Children.Remove(marker);
            }
            // Clear the dictionary to remove references
            spriteMarkers.Clear();
        }

        /// <summary>
        /// Creates a visual marker for a single sprite
        /// This marker shows the sprite's position and boundaries on the canvas
        /// </summary>
        /// <param name="sprite">The sprite data to create a marker for</param>
        private void CreateSpriteMarker(SpriteItem sprite)
        {
            if (loadedImage == null) return;

            // COMPONENT CONNECTION:
            // Get the unique color assigned to this sprite from the collection
            Color markerColor = jsonDataEntry.SpriteCollection.GetItemColor(sprite.Id);

            // Create a new visual marker for this sprite
            var marker = new SpriteItemMarker(sprite, markerColor);
            
            // COMPONENT CONNECTION:
            // Subscribe to the marker's selection event
            // When a user clicks the marker, this event will fire
            marker.MarkerSelected += Marker_Selected;

            // Add the marker to the canvas and tracking dictionary
            imageCanvas.Children.Add(marker);
            spriteMarkers[sprite.Id] = marker;

            // Update the marker's appearance based on whether it's selected
            marker.UpdateAppearance(sprite == jsonDataEntry.SpriteCollection.SelectedItem);

            // Update the marker's position based on the current zoom level
            marker.UpdatePosition(currentZoom);
        }

        /// <summary>
        /// Event handler for when a sprite marker is selected by clicking
        /// This synchronizes the selection in the data entry control
        /// </summary>
        /// <param name="sender">The marker that was clicked</param>
        /// <param name="sprite">The sprite data associated with the marker</param>
        private void Marker_Selected(object sender, SpriteItem sprite)
        {
            // COMPONENT CONNECTION:
            // Update the JsonDataEntryControl to show this sprite as selected
            jsonDataEntry.SpriteCollection.SelectedItem = sprite;
        }

        /// <summary>
        /// Event handler for when the selected sprite changes in the JsonDataEntryControl
        /// Updates the visual appearance of all markers to reflect the new selection
        /// </summary>
        /// <param name="sender">The JsonDataEntryControl</param>
        /// <param name="sprite">The newly selected sprite</param>
        private void OnSelectedSpriteChanged(object sender, SpriteItem sprite)
        {
            // Update all markers to show which one is selected
            foreach (var marker in spriteMarkers.Values)
            {
                // Tell each marker whether it's selected or not
                marker.UpdateAppearance(marker.SpriteItem == sprite);
            }
        }

        /// <summary>
        /// Event handler for when a new sprite is added in the JsonDataEntryControl
        /// Creates a new visual marker for the sprite
        /// </summary>
        /// <param name="sender">The JsonDataEntryControl</param>
        /// <param name="sprite">The newly added sprite</param>
        private void OnSpriteAdded(object sender, SpriteItem sprite)
        {
            if (loadedImage != null)
            {
                // Create a visual marker for the new sprite
                CreateSpriteMarker(sprite);
            }
        }

        /// <summary>
        /// Event handler for when a sprite is removed in the JsonDataEntryControl
        /// Removes the corresponding visual marker
        /// </summary>
        /// <param name="sender">The JsonDataEntryControl</param>
        /// <param name="sprite">The removed sprite</param>
        private void OnSpriteRemoved(object sender, SpriteItem sprite)
        {
            // Check if we have a marker for this sprite
            if (spriteMarkers.TryGetValue(sprite.Id, out var marker))
            {
                // Remove the marker from the canvas
                imageCanvas.Children.Remove(marker);
                // Remove it from our tracking dictionary
                spriteMarkers.Remove(sprite.Id);
            }
        }

        /// <summary>
        /// Converts a mouse position to precise pixel coordinates
        /// Accounts for zoom level and ensures coordinates are whole numbers
        /// </summary>
        /// <param name="point">The raw mouse position</param>
        /// <returns>The corresponding pixel coordinates on the sprite atlas</returns>
        public Point SnapToPixel(Point point)
        {
            // Convert mouse position from zoomed canvas coordinates to original image coordinates
            point.X /= currentZoom;
            point.Y /= currentZoom;
            // Use Math.Floor to ensure we get whole pixel values
            return new Point(Math.Floor(point.X), Math.Floor(point.Y));
        }

        /// <summary>
        /// Event handler for mouse click on the image canvas
        /// Begins tracking pixel location for sprite placement
        /// </summary>
        private void imageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (loadedImage == null) return;

            // Convert the mouse position to precise pixel coordinates
            currentPixelLocation = SnapToPixel(e.GetPosition(imageCanvas));

            // Start tracking mouse movement
            isTrackingPixel = true;

            // COMPONENT CONNECTION:
            // Update and show the pixel location display
            if (pixelLocationDisplay != null)
            {
                pixelLocationDisplay.UpdatePosition(currentPixelLocation, currentZoom);
                pixelLocationDisplay.Show();

                // COMPONENT CONNECTION:
                // Update the selected sprite's position in the JsonDataEntryControl
                jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                // Update the visual marker for the selected sprite
                UpdateSelectedSpriteMarker();
            }

            // Update status bar with current position
            UpdateSelectionStatus(currentPixelLocation.X, currentPixelLocation.Y, 0, 0);

            e.Handled = true; // Prevent event from bubbling up
        }

        /// <summary>
        /// Event handler for mouse movement on the image canvas
        /// Tracks pixel location for precise sprite placement
        /// </summary>
        private void imageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (loadedImage == null) return;

            // Convert the mouse position to precise pixel coordinates
            Point mousePosition = SnapToPixel(e.GetPosition(imageCanvas));

            // Always update status bar with current mouse position
            UpdateSelectionStatus(mousePosition.X, mousePosition.Y, 0, 0);

            // If we're actively tracking (after mouse down), update positions
            if (isTrackingPixel)
            {
                currentPixelLocation = mousePosition;

                // BOUNDARY CHECKING:
                // Keep the pixel location within the bounds of the image
                // This prevents selecting pixels outside the sprite atlas
                currentPixelLocation.X = Math.Max(0, Math.Min(currentPixelLocation.X, imageCanvas.Width / currentZoom - 1));
                currentPixelLocation.Y = Math.Max(0, Math.Min(currentPixelLocation.Y, imageCanvas.Height / currentZoom - 1));

                // COMPONENT CONNECTION:
                // Update the pixel location display with new coordinates
                if (pixelLocationDisplay != null)
                {
                    pixelLocationDisplay.UpdatePosition(currentPixelLocation, currentZoom);

                    // COMPONENT CONNECTION:
                    // Update the selected sprite's position in the JsonDataEntryControl
                    jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                    // Update the visual marker for the selected sprite
                    UpdateSelectedSpriteMarker();
                }
            }
        }

        /// <summary>
        /// Updates the position of the selected sprite's marker
        /// Called when the sprite's position changes through mouse interaction
        /// </summary>
        private void UpdateSelectedSpriteMarker()
        {
            // COMPONENT CONNECTION:
            // Get the currently selected sprite from JsonDataEntryControl
            var selectedSprite = jsonDataEntry.SpriteCollection.SelectedItem;
            
            // If there's a selected sprite and we have a marker for it
            if (selectedSprite != null && spriteMarkers.TryGetValue(selectedSprite.Id, out var marker))
            {
                // Update the marker's position based on the current zoom level
                marker.UpdatePosition(currentZoom);
            }
        }

        /// <summary>
        /// Event handler for mouse button release on the image canvas
        /// Finalizes sprite placement after mouse tracking
        /// </summary>
        private void imageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isTrackingPixel)
            {
                // Stop tracking pixel movement
                isTrackingPixel = false;

                // Keep pixel location display visible
                if (pixelLocationDisplay != null)
                {
                    pixelLocationDisplay.Show();
                }

                // COMPONENT CONNECTION:
                // Update the JsonDataEntryControl with the final location
                jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                // Update the visual marker for the selected sprite
                UpdateSelectedSpriteMarker();
            }
        }

        /// <summary>
        /// Legacy method kept for compatibility with older code
        /// No longer used in current version
        /// </summary>
        public void UpdateHandlePositions()
        {
            // Method kept for compatibility
        }

        /// <summary>
        /// Legacy method kept for compatibility with older code
        /// No longer used in current version
        /// </summary>
        private void btnAddSelection_Click(object sender, RoutedEventArgs e)
        {
            // Not needed with the new approach
        }

        /// <summary>
        /// Event handler for the "Save Atlas" button click
        /// Saves all sprite data to a JSON file
        /// </summary>
        private void btnSaveAtlas_Click(object sender, RoutedEventArgs e)
        {
            // Check if an image is loaded
            if (loadedImage == null)
            {
                MessageBox.Show("Please load an image before saving.",
                    "Atlas Plotter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create and configure a file save dialog
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Atlas JSON",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                // Default filename based on the loaded image name
                FileName = System.IO.Path.GetFileNameWithoutExtension(imagePath) + "_atlas"
            };

            // Show the dialog and process the result if user chose a location
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // DATA FLOW:
                    // Configure JSON serialization options (pretty-printed JSON)
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    // COMPONENT CONNECTION:
                    // Serialize the entire sprite collection from JsonDataEntryControl
                    string json = JsonSerializer.Serialize(jsonDataEntry.SpriteCollection, options);
                    
                    // Write the JSON to the selected file
                    File.WriteAllText(saveFileDialog.FileName, json);

                    // Show a success message
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

        /// <summary>
        /// Event handler for the "Clear Selections" button click
        /// Removes all sprite markers from the canvas
        /// </summary>
        private void btnClearSelections_Click(object sender, RoutedEventArgs e)
        {
            ClearSelections();
        }

        /// <summary>
        /// Clears all sprites and visual elements from the canvas
        /// Keeps the loaded image intact
        /// </summary>
        private void ClearSelections()
        {
            // Remove all canvas elements except the core components
            var children = imageCanvas.Children.Cast<UIElement>().ToList();
            foreach (var child in children)
            {
                if (child != displayImage && child != pixelLocationDisplay && !(child is SpriteItemMarker))
                {
                    imageCanvas.Children.Remove(child);
                }
            }

            // Clear all sprite markers
            ClearSpriteMarkers();

            // Clear legacy atlas items
            atlasItems.Clear();

            // Hide the pixel location display
            if (pixelLocationDisplay != null)
            {
                pixelLocationDisplay.Hide();
            }
        }

        /// <summary>
        /// Updates the status bar with current selection information
        /// Shows pixel coordinates and dimensions in the UI
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="width">Selection width (if applicable)</param>
        /// <param name="height">Selection height (if applicable)</param>
        /// <param name="clear">Whether to clear the status display</param>
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
                    // Show just the coordinates in the status bar
                    tbSelectionInfo.Text = $"X: {(int)x}, Y: {(int)y}";
                }
            }
        }

        #region Zoom Functionality

        /// <summary>
        /// Event handler for the "Zoom In" button click
        /// Increases zoom by one increment
        /// </summary>
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ApplyZoom(currentZoom + ZoomIncrement);
        }

        /// <summary>
        /// Event handler for the "Zoom Out" button click
        /// Decreases zoom by one increment
        /// </summary>
        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ApplyZoom(currentZoom - ZoomIncrement);
        }

        /// <summary>
        /// Event handler for the "Reset Zoom" button click
        /// Returns zoom to 100%
        /// </summary>
        private void btnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        /// <summary>
        /// Resets the zoom to 100% (1.0)
        /// </summary>
        private void ResetZoom()
        {
            ApplyZoom(1.0);
        }

        /// <summary>
        /// Applies a specific zoom level to the canvas
        /// Updates all UI elements to reflect the new zoom
        /// </summary>
        /// <param name="zoom">The zoom level to apply (1.0 = 100%)</param>
        private void ApplyZoom(double zoom)
        {
            // BOUNDARY CHECKING:
            // Ensure zoom level stays within defined limits
            zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));

            // Apply the zoom level to our tracked value
            currentZoom = zoom;
            
            // COMPONENT CONNECTION:
            // Apply zoom to the ScaleTransform in XAML that scales the canvas
            zoomTransform.ScaleX = zoom;
            zoomTransform.ScaleY = zoom;

            // Update zoom level display in UI
            UpdateZoomDisplay();

            // COMPONENT CONNECTION:
            // Update pixel location display if visible
            // This ensures the display is correctly positioned at the new zoom level
            if (pixelLocationDisplay != null && pixelLocationDisplay.Visibility == Visibility.Visible)
            {
                pixelLocationDisplay.UpdatePosition(pixelLocationDisplay.CurrentLocation, currentZoom);
            }

            // COMPONENT CONNECTION:
            // Update all sprite markers with new zoom level
            // This ensures markers are correctly sized and positioned
            foreach (var marker in spriteMarkers.Values)
            {
                marker.UpdatePosition(currentZoom);
            }
        }

        /// <summary>
        /// Updates the zoom level text display in the UI
        /// </summary>
        private void UpdateZoomDisplay()
        {
            txtZoomLevel.Text = $"{currentZoom * 100:0}%";
        }

        /// <summary>
        /// Event handler for mouse wheel movement on the ScrollViewer
        /// Enables zooming with Ctrl+Mouse Wheel
        /// </summary>
        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Check if Ctrl key is held while scrolling
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // USER EXPERIENCE ENHANCEMENT:
                // Calculate zoom factor based on wheel direction
                // Scrolling up zooms in, scrolling down zooms out
                double zoomFactor = e.Delta > 0 ? ZoomIncrement : -ZoomIncrement;
                double newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, currentZoom + zoomFactor));

                // Apply the zoom
                ApplyZoom(newZoom);

                // Prevent the ScrollViewer from scrolling vertically
                // We want to zoom, not scroll, when Ctrl is pressed
                e.Handled = true;
            }
        }
        
        #endregion
    }

    /// <summary>
    /// Legacy class representing an individual sprite item in the atlas
    /// Mostly replaced by the more comprehensive SpriteItem class in JsonDataModels.cs
    /// Kept for backward compatibility
    /// </summary>
    public class AtlasItem
    {
        /// <summary>Name of the sprite</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>X position within the atlas</summary>
        public int X { get; set; }
        
        /// <summary>Y position within the atlas</summary>
        public int Y { get; set; }
        
        /// <summary>Width of the sprite in pixels</summary>
        public int Width { get; set; }
        
        /// <summary>Height of the sprite in pixels</summary>
        public int Height { get; set; }
    }

    /// <summary>
    /// Legacy class representing the entire sprite atlas
    /// Mostly replaced by SpriteItemCollection in SpriteItemCollection.cs
    /// Kept for backward compatibility
    /// </summary>
    public class Atlas
    {
        /// <summary>Path to the atlas image file</summary>
        public string ImagePath { get; set; } = string.Empty;
        
        /// <summary>Width of the atlas image in pixels</summary>
        public int ImageWidth { get; set; }
        
        /// <summary>Height of the atlas image in pixels</summary>
        public int ImageHeight { get; set; }
        
        /// <summary>Collection of sprite frames defined in the atlas</summary>
        public List<AtlasItem> Frames { get; set; } = new List<AtlasItem>();
    }
}