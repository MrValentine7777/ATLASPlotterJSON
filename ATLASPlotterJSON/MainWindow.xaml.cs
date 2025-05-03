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

        /// <summary>Path to the currently loaded image file</summary>
        private string? imagePath;

        /// <summary>
        /// Dictionary mapping sprite IDs to their visual markers on the canvas
        /// This allows quick lookup of markers when sprites need to be updated
        /// </summary>
        internal readonly Dictionary<int, SpriteItemMarker> spriteMarkers = [];

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

        // PANNING PROPERTIES
        /// <summary>True when actively panning the image</summary>
        private bool isPanning = false;

        /// <summary>The starting point of a pan operation</summary>
        private Point panStartPoint;

        /// <summary>The current panning offset</summary>
        private Point panOffset = new Point(0, 0);

        /// <summary>
        /// Cached JSON serializer options for saving JSON (serialization).
        /// Configured to create nicely formatted, human-readable JSON files.
        /// </summary>
        private static readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        /// <summary>
        /// Zoom viewer component for detailed inspection
        /// </summary>
        private ZoomViewer? zoomViewer;

        /// <summary>
        /// Gets the loaded sprite atlas image
        /// </summary>
        public BitmapImage? LoadedImage => loadedImage;

        /// <summary>
        /// Gets the image control that displays the loaded image
        /// </summary>
        public Image DisplayImage => displayImage;

        /// <summary>
        /// Gets the main image canvas for external access
        /// </summary>
        public Canvas MainImageCanvas => this.imageCanvas;

        /// <summary>
        /// Event fired when sprite markers are updated
        /// </summary>
        public event EventHandler MarkersUpdated;

        /// <summary>
        /// Initialize the main application window
        /// </summary>
        public MainWindow()
        {
            // Initialize the WPF components defined in XAML
            InitializeComponent();

            // Initialize command management system
            InitializeCommandManagement();

            // COMPONENT CONNECTION:
            // Connect to JsonDataEntryControl events to stay synchronized
            // These events notify us when sprites are added, removed, or selected
            jsonDataEntry.SelectedSpriteChanged += OnSelectedSpriteChanged!;
            jsonDataEntry.SpriteAdded += OnSpriteAdded!;
            jsonDataEntry.SpriteRemoved += OnSpriteRemoved!;
            jsonDataEntry.SpritePropertyChanged += OnSpritePropertyChanged!;

            // Subscribe to the window size changed event
            SizeChanged += Window_SizeChanged;

            // Initialize the zoom viewer
            zoomViewer = new ZoomViewer(this);

            // Set the zoom viewer as the child of the zoomViewerContainer instead of adding to imageCanvas
            zoomViewerContainer.Child = zoomViewer;

            // Set the visibility based on the checkbox
            zoomViewerContainer.Visibility = chkShowZoomViewer.IsChecked == true ? 
                Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Initialize the command management system for the window.
        /// </summary>
        private void InitializeCommandManagement()
        {
            // Register keyboard shortcuts
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, 
                (s, e) => Commands.CommandManager.Instance.Undo(),
                (s, e) => e.CanExecute = Commands.CommandManager.Instance.CanUndo));
                
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, 
                (s, e) => Commands.CommandManager.Instance.Redo(),
                (s, e) => e.CanExecute = Commands.CommandManager.Instance.CanRedo));
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
                    btnUpdateMarkers.IsEnabled = true;  // Enable the Update Markers button

                    // 4. Create the pixel location tracker for precise positioning
                    InitializePixelLocationDisplay();

                    // 5. Create visual markers for any sprites already in the collection
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

            // Update window title to show which image is loaded
            Title = $"Atlas Plotter - {System.IO.Path.GetFileName(path)}";

            // Update the scaling to fit the image in the available space
            UpdateImageScaling();
            
            // Update the zoom viewer with the loaded image if it's visible
            if (zoomViewer != null && chkShowZoomViewer.IsChecked == true)
            {
                zoomViewer.UpdateContent();
            }

            // Clear command history when loading a new image
            Commands.CommandManager.Instance.ClearHistory();
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
            marker.MarkerSelected += Marker_Selected!;

            // Add the marker to the canvas and tracking dictionary
            imageCanvas.Children.Add(marker);
            spriteMarkers[sprite.Id] = marker;

            // Update the marker's appearance based on whether it's selected
            marker.UpdateAppearance(sprite == jsonDataEntry.SpriteCollection.SelectedItem);

            // Update the marker's position
            marker.UpdatePosition();
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
        /// Event handler for when a sprite property changes in the JsonDataEntryControl
        /// Updates the visual marker on the canvas to reflect the changes
        /// </summary>
        /// <param name="sender">The JsonDataEntryControl</param>
        /// <param name="sprite">The sprite with changed properties</param>
        private void OnSpritePropertyChanged(object sender, SpriteItem sprite)
        {
            // Update the sprite's visual representation
            UpdateSelectedSpriteMarker();
            
            // Notify listeners that markers have been updated
            MarkersUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Converts a mouse position to precise pixel coordinates
        /// Accounts for image panning offset
        /// </summary>
        /// <param name="point">The raw mouse position</param>
        /// <returns>The corresponding pixel coordinates on the sprite atlas</returns>
        public Point SnapToPixel(Point point)
        {
            if (loadedImage == null) return point;

            // Calculate the offset from the edge of the canvas to the image
            double offsetX = Canvas.GetLeft(displayImage);
            double offsetY = Canvas.GetTop(displayImage);

            // At 1:1 scaling, the conversion is simplified
            // We just need to account for the panning offset
            double imageX = point.X - offsetX;
            double imageY = point.Y - offsetY;

            // Use Math.Floor to ensure we get whole pixel values and constrain to image bounds
            imageX = Math.Floor(Math.Max(0, Math.Min(imageX, loadedImage.Width - 1)));
            imageY = Math.Floor(Math.Max(0, Math.Min(imageY, loadedImage.Height - 1)));

            return new Point(imageX, imageY);
        }

        /// <summary>
        /// Event handler for mouse click on the image canvas
        /// Begins tracking pixel location for sprite placement or starts panning
        /// </summary>
        private void imageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (loadedImage == null) return;

            // If holding the middle mouse button or control key, start panning
            if (e.MiddleButton == MouseButtonState.Pressed || Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                StartPanning(e.GetPosition(imageCanvas));
                e.Handled = true;
                return;
            }

            // Convert the mouse position to precise pixel coordinates
            currentPixelLocation = SnapToPixel(e.GetPosition(imageCanvas));

            // Start tracking mouse movement
            isTrackingPixel = true;

            // Update and show the pixel location display
            if (pixelLocationDisplay != null)
            {
                pixelLocationDisplay.UpdatePosition(currentPixelLocation);
                pixelLocationDisplay.Show();

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
        /// Begins panning the image
        /// </summary>
        private void StartPanning(Point startPoint)
        {
            isPanning = true;
            panStartPoint = startPoint;
            imageCanvas.Cursor = Cursors.Hand;
            imageCanvas.CaptureMouse(); // Capture mouse to track movement even outside canvas
        }

        /// <summary>
        /// Event handler for mouse movement on the image canvas
        /// Tracks pixel location for sprite placement or pans the image
        /// </summary>
        private void imageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (loadedImage == null) return;

            Point currentMousePos = e.GetPosition(imageCanvas);

            // Handle panning if active
            if (isPanning)
            {
                // Calculate the movement delta since starting to pan
                Vector offset = currentMousePos - panStartPoint;
                
                // Get available space
                var mainGrid = (Grid)Content;
                double availableWidth = (mainGrid.ColumnDefinitions[0].ActualWidth - 20);
                double availableHeight = (mainGrid.RowDefinitions[1].ActualHeight - 20);

                // Calculate new position with delta movement
                double newPosX = panOffset.X + offset.X;
                double newPosY = panOffset.Y + offset.Y;

                // Apply boundary constraints
                newPosX = EnforcePanningBoundaryX(newPosX, availableWidth);
                newPosY = EnforcePanningBoundaryY(newPosY, availableHeight);

                // Update display position
                Canvas.SetLeft(displayImage, newPosX);
                Canvas.SetTop(displayImage, newPosY);

                // Update the pixel location display if visible
                if (pixelLocationDisplay != null && pixelLocationDisplay.Visibility == Visibility.Visible)
                {
                    pixelLocationDisplay.UpdatePosition(pixelLocationDisplay.CurrentLocation);
                }

                // Update all sprite markers to match the new positioning
                foreach (var marker in spriteMarkers.Values)
                {
                    marker.UpdatePosition();
                }

                // Update selection handles
                UpdateHandlePositions();

                e.Handled = true;
                return;
            }

            // Convert the mouse position to precise pixel coordinates
            Point mousePosition = SnapToPixel(currentMousePos);

            // Always update status bar with current mouse position
            UpdateSelectionStatus(mousePosition.X, mousePosition.Y, 0, 0);

            // If we're actively tracking (after mouse down), update positions
            if (isTrackingPixel)
            {
                currentPixelLocation = mousePosition;

                // Keep the pixel location within the bounds of the image
                currentPixelLocation.X = Math.Max(0, Math.Min(currentPixelLocation.X, loadedImage.Width - 1));
                currentPixelLocation.Y = Math.Max(0, Math.Min(currentPixelLocation.Y, loadedImage.Height - 1));

                // Update the pixel location display with new coordinates
                if (pixelLocationDisplay != null)
                {
                    pixelLocationDisplay.UpdatePosition(currentPixelLocation);

                    // Update the selected sprite's position in the JsonDataEntryControl
                    jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                    // Update the visual marker for the selected sprite
                    UpdateSelectedSpriteMarker();
                }
            }
        }

        /// <summary>
        /// Event handler for mouse button release on the image canvas
        /// Finalizes sprite placement or panning operation
        /// </summary>
        private void imageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isPanning)
            {
                // End panning and save the current position
                isPanning = false;
                Point currentMousePos = e.GetPosition(imageCanvas);
                
                // Calculate the final offset from the starting point
                Vector offset = currentMousePos - panStartPoint;
                
                // Update stored panning offset
                panOffset.X += offset.X;
                panOffset.Y += offset.Y;
                
                // Get available space
                var mainGrid = (Grid)Content;
                double availableWidth = (mainGrid.ColumnDefinitions[0].ActualWidth - 20);
                double availableHeight = (mainGrid.RowDefinitions[1].ActualHeight - 20);

                // Apply boundary constraints to the final position
                panOffset.X = EnforcePanningBoundaryX(panOffset.X, availableWidth);
                panOffset.Y = EnforcePanningBoundaryY(panOffset.Y, availableHeight);
                
                // Release the mouse capture
                imageCanvas.ReleaseMouseCapture();
                imageCanvas.Cursor = Cursors.Arrow;
                e.Handled = true;
                return;
            }

            if (isTrackingPixel)
            {
                // Stop tracking pixel movement
                isTrackingPixel = false;

                // Keep pixel location display visible
                if (pixelLocationDisplay != null)
                {
                    pixelLocationDisplay.Show();
                }

                // Update the JsonDataEntryControl with the final location
                jsonDataEntry.SetCurrentLocation(currentPixelLocation);

                // Update the visual marker for the selected sprite
                UpdateSelectedSpriteMarker();
            }
        }

        /// <summary>
        /// Updates the position of the selected sprite's marker
        /// Called when the sprite's position changes through mouse interaction
        /// </summary>
        public void UpdateSelectedSpriteMarker()
        {
            // COMPONENT CONNECTION:
            // Get the currently selected sprite from JsonDataEntryControl
            var selectedSprite = jsonDataEntry.SpriteCollection.SelectedItem;

            // If there's a selected sprite and we have a marker for it
            if (selectedSprite != null && spriteMarkers.TryGetValue(selectedSprite.Id, out var marker))
            {
                // Update the marker's position
                marker.UpdatePosition();
            }

            // Also update all selection handles
            UpdateHandlePositions();
        }

        /// <summary>
        /// Updates the positions of all selection handles based on their target rectangles.
        /// Called whenever a rectangle's position or dimensions change.
        /// </summary>
        public void UpdateHandlePositions()
        {
            // Calculate the current zoom level based on image scaling
            double zoomLevel = 1.0;
            if (loadedImage != null && displayImage != null)
            {
                zoomLevel = displayImage.Width / loadedImage.Width;
            }

            // Find all SelectionHandle elements in the canvas and update their positions
            foreach (var element in imageCanvas.Children)
            {
                if (element is SelectionHandle handle)
                {
                    // Update the handle position with the current zoom level
                    handle.UpdatePosition(zoomLevel);
                }
            }
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
                    // Using cached serializer options instead of creating new ones
                    
                    // COMPONENT CONNECTION:
                    // Serialize the entire sprite collection from JsonDataEntryControl
                    string json = JsonSerializer.Serialize(jsonDataEntry.SpriteCollection, _serializeOptions);

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

            // Hide the pixel location display
            if (pixelLocationDisplay != null)
            {
                pixelLocationDisplay.Hide();
            }
        }

        /// <summary>
        /// Event handler for the "Update Markers" button click
        /// Recreates all sprite markers on the canvas to ensure their visual representation matches the data
        /// </summary>
        private void btnUpdateMarkers_Click(object sender, RoutedEventArgs e)
        {
            // Only proceed if an image is loaded
            if (loadedImage == null)
            {
                MessageBox.Show("Please load an image before updating markers.",
                    "Atlas Plotter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Update status message
                UpdateSelectionStatus(0, 0, 0, 0, true);
                tbSelectionInfo.Text = "Updating sprite markers...";

                // Recreate all markers for existing sprites
                CreateMarkersForExistingSprites();

                // Notify listeners that markers have been updated
                MarkersUpdated?.Invoke(this, EventArgs.Empty);

                // Show a success message
                tbSelectionInfo.Text = $"Successfully updated {spriteMarkers.Count} sprite markers.";
            }
            catch (Exception ex)
            {
                // Show an error message if something goes wrong
                MessageBox.Show($"Error updating markers: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                tbSelectionInfo.Text = "Error updating markers.";
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

        /// <summary>
        /// Event handler for the window size changed event
        /// Updates the image and UI elements to fill available space
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only proceed if an image is loaded
            if (loadedImage != null)
            {
                UpdateImageScaling();
            }
        }

        /// <summary>
        /// Updates the image display to show it at 1:1 scale
        /// and repositions all UI elements accordingly
        /// </summary>
        private void UpdateImageScaling()
        {
            if (loadedImage == null) return;

            // Get a reference to the main Grid that's defined in the XAML
            var mainGrid = (Grid)Content;

            // Calculate the available display area (accounting for margins)
            double availableWidth = (mainGrid.ColumnDefinitions[0].ActualWidth - 20);
            double availableHeight = (mainGrid.RowDefinitions[1].ActualHeight - 20);

            // Set the canvas dimensions to match available space
            imageCanvas.Width = availableWidth;
            imageCanvas.Height = availableHeight;

            // Use 1:1 pixel ratio (actual size) instead of scaling to fit
            displayImage.Width = loadedImage.Width;
            displayImage.Height = loadedImage.Height;

            // Apply any existing pan offset, or center the image if it fits within the canvas
            double leftPosition = panOffset.X;
            double topPosition = panOffset.Y;

            // If the image is smaller than the canvas, center it
            if (displayImage.Width < availableWidth)
            {
                leftPosition = (availableWidth - displayImage.Width) / 2;
                panOffset.X = leftPosition; // Save the centered position
            }
            
            if (displayImage.Height < availableHeight)
            {
                topPosition = (availableHeight - displayImage.Height) / 2;
                panOffset.Y = topPosition; // Save the centered position
            }

            // Apply boundary constraints to prevent panning beyond the image edges
            leftPosition = EnforcePanningBoundaryX(leftPosition, availableWidth);
            topPosition = EnforcePanningBoundaryY(topPosition, availableHeight);
            
            // Update the stored pan offset
            panOffset.X = leftPosition;
            panOffset.Y = topPosition;

            // Position the image according to the pan offset
            Canvas.SetLeft(displayImage, leftPosition);
            Canvas.SetTop(displayImage, topPosition);

            // Update the pixel location display
            if (pixelLocationDisplay != null && pixelLocationDisplay.Visibility == Visibility.Visible)
            {
                pixelLocationDisplay.UpdatePosition(pixelLocationDisplay.CurrentLocation);
            }

            // Update all sprite markers to match the new positioning
            foreach (var marker in spriteMarkers.Values)
            {
                // Update the marker's position
                marker.UpdatePosition();
                
                // Also update appearance to ensure selected state is correctly displayed
                marker.UpdateAppearance(marker.SpriteItem == jsonDataEntry.SpriteCollection.SelectedItem);
            }
            
            // Update any selection handles
            UpdateHandlePositions();
        }

        /// <summary>
        /// Ensures the X panning position stays within valid boundaries
        /// </summary>
        private double EnforcePanningBoundaryX(double positionX, double availableWidth)
        {
            if (loadedImage == null) return positionX;
            
            // If the image is smaller than the canvas, don't adjust (it will be centered)
            if (loadedImage.Width <= availableWidth) return positionX;
            
            // Enforce left boundary: don't allow panning to show space to the left of the image
            if (positionX > 0) return 0;
            
            // Enforce right boundary: don't allow panning to show space to the right of the image
            double minX = availableWidth - loadedImage.Width;
            if (positionX < minX) return minX;
            
            return positionX;
        }

        /// <summary>
        /// Ensures the Y panning position stays within valid boundaries
        /// </summary>
        private double EnforcePanningBoundaryY(double positionY, double availableHeight)
        {
            if (loadedImage == null) return positionY;
            
            // If the image is smaller than the canvas, don't adjust (it will be centered)
            if (loadedImage.Height <= availableHeight) return positionY;
            
            // Enforce top boundary: don't allow panning to show space above the image
            if (positionY > 0) return 0;
            
            // Enforce bottom boundary: don't allow panning to show space below the image
            double minY = availableHeight - loadedImage.Height;
            if (positionY < minY) return minY;
            
            return positionY;
        }

        /// <summary>
        /// Handles mouse wheel event for easy panning
        /// </summary>
        private void imageCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (loadedImage == null) return;

            // Only use mouse wheel for panning if the Ctrl key is pressed
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                // Get available space
                var mainGrid = (Grid)Content;
                double availableWidth = (mainGrid.ColumnDefinitions[0].ActualWidth - 20);
                double availableHeight = (mainGrid.RowDefinitions[1].ActualHeight - 20);

                // Calculate panning amount based on wheel delta
                double panAmount = e.Delta / 3.0; // Adjust this value to change pan sensitivity
                
                // Calculate new position based on wheel direction
                // If Shift is held, pan horizontally instead of vertically
                double newPosX = panOffset.X;
                double newPosY = panOffset.Y;
                
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    // Pan horizontally
                    newPosX += panAmount;
                }
                else
                {
                    // Pan vertically
                    newPosY += panAmount;
                }

                // Apply boundary constraints
                newPosX = EnforcePanningBoundaryX(newPosX, availableWidth);
                newPosY = EnforcePanningBoundaryY(newPosY, availableHeight);

                // Save the new position
                panOffset.X = newPosX;
                panOffset.Y = newPosY;
                
                // Update display position
                Canvas.SetLeft(displayImage, newPosX);
                Canvas.SetTop(displayImage, newPosY);

                // Update the UI elements
                if (pixelLocationDisplay != null && pixelLocationDisplay.Visibility == Visibility.Visible)
                {
                    pixelLocationDisplay.UpdatePosition(pixelLocationDisplay.CurrentLocation);
                }

                foreach (var marker in spriteMarkers.Values)
                {
                    marker.UpdatePosition();
                }

                UpdateHandlePositions();
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event handler for the "Show Zoom Viewer" checkbox
        /// Toggles the visibility of the zoom viewer component
        /// </summary>
        private void chkShowZoomViewer_Click(object sender, RoutedEventArgs e)
        {
            if (zoomViewer != null)
            {
                // Show or hide the zoom viewer container based on checkbox state
                zoomViewerContainer.Visibility = chkShowZoomViewer.IsChecked == true ? 
                    Visibility.Visible : Visibility.Collapsed;
                    
                // Update the zoom viewer content if it's becoming visible
                if (chkShowZoomViewer.IsChecked == true && loadedImage != null)
                {
                    zoomViewer.UpdateContent();
                }
            }
        }
    }
}