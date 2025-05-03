using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// A docked zoom viewer control that allows zooming into the atlas image
    /// while keeping the main view unchanged.
    /// </summary>
    public class ZoomViewer : UserControl
    {
        // Constants for the zoom viewer appearance and behavior
        private const double DEFAULT_HEIGHT = 150.0;
        private const double MIN_ZOOM = 1.0;
        private const double MAX_ZOOM = 50.0;
        private const double ZOOM_STEP = 0.25;
        private const double BORDER_THICKNESS = 1.0;
        private const double DEFAULT_ZOOM = 1.0; // Changed default zoom to 1.0 for pixel-perfect match

        // Reference to the main window
        private readonly MainWindow parentWindow;

        // Zoom viewer components
        private readonly Border border;
        private readonly Canvas contentCanvas;
        private readonly Image zoomedImage;
        private readonly Rectangle viewportIndicator;
        private readonly StackPanel zoomControls;
        private readonly TextBlock zoomLevelText;

        // Current zoom state
        private double currentZoom = DEFAULT_ZOOM; // Initialize to 1.0 for pixel-perfect match
        private Point panOffset = new Point(0, 0);
        private bool isDragging = false;
        private Point lastMousePos;

        // Track whether content is initialized
        private bool contentInitialized = false;

        // Dictionary to track SpriteItemMarkers in the zoom viewer
        private readonly Dictionary<int, SpriteItemMarker> zoomMarkers = new Dictionary<int, SpriteItemMarker>();

        /// <summary>
        /// Gets the current zoom level of the zoom viewer
        /// </summary>
        public double CurrentZoom => currentZoom;

        /// <summary>
        /// Creates a new zoom viewer attached to the parent window
        /// </summary>
        /// <param name="parent">The main window that owns this zoom viewer</param>
        public ZoomViewer(MainWindow parent)
        {
            parentWindow = parent;
            
            // Create the main grid container
            Grid mainGrid = new Grid();
            
            // Set up the main container with a border
            border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(225, 240, 240, 240)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(BORDER_THICKNESS)
            };

            // Set up the canvas for the zoomed content
            contentCanvas = new Canvas
            {
                ClipToBounds = true,
                Background = Brushes.LightGray
            };

            // Set up the zoomed image
            zoomedImage = new Image
            {
                Stretch = Stretch.None,
                RenderTransformOrigin = new Point(0, 0)
            };

            // Set attached properties correctly for pixel-perfect rendering
            RenderOptions.SetBitmapScalingMode(zoomedImage, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(zoomedImage, EdgeMode.Aliased);

            // Create viewport indicator to show the current view area
            viewportIndicator = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)),
                Visibility = Visibility.Collapsed
            };

            // Create zoom controls panel
            zoomControls = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 5, 0)
            };

            // Create label
            TextBlock labelText = new TextBlock
            {
                Text = "Zoom Viewer",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 3, 10, 0)
            };

            // Create zoom buttons
            Button zoomOutBtn = new Button { Content = "-", Width = 24, Padding = new Thickness(0) };
            zoomLevelText = new TextBlock 
            { 
                Text = $"{currentZoom:F1}×", 
                VerticalAlignment = VerticalAlignment.Center,
                Width = 35,
                TextAlignment = TextAlignment.Center
            };
            Button zoomInBtn = new Button { Content = "+", Width = 24, Padding = new Thickness(0) };
            Button resetBtn = new Button { Content = "R", Width = 24, Padding = new Thickness(0) };
            // Add a new button specifically for 1:1 pixel match
            Button oneToOneBtn = new Button { 
                Content = "1:1", 
                Width = 30, 
                Padding = new Thickness(0),
                ToolTip = "Set to exact 1:1 pixel match"
            };

            // Add handlers for zoom buttons
            zoomOutBtn.Click += (s, e) => AdjustZoom(-ZOOM_STEP);
            zoomInBtn.Click += (s, e) => AdjustZoom(ZOOM_STEP);
            resetBtn.Click += (s, e) => ResetZoom();
            oneToOneBtn.Click += (s, e) => SetOneToOneZoom();

            // Add controls to the zoom panel
            zoomControls.Children.Add(zoomOutBtn);
            zoomControls.Children.Add(zoomLevelText);
            zoomControls.Children.Add(zoomInBtn);
            zoomControls.Children.Add(oneToOneBtn); // Add the new 1:1 button
            zoomControls.Children.Add(resetBtn);

            // Add components to the canvas
            contentCanvas.Children.Add(zoomedImage);
            contentCanvas.Children.Add(viewportIndicator);

            // Create a grid for layout
            Grid containerGrid = new Grid();
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Add top panel with controls
            DockPanel topPanel = new DockPanel { LastChildFill = true };
            topPanel.Children.Add(zoomControls);
            DockPanel.SetDock(zoomControls, Dock.Right);
            topPanel.Children.Add(labelText);
            
            Grid.SetRow(topPanel, 0);
            Grid.SetRow(contentCanvas, 1);
            
            containerGrid.Children.Add(topPanel);
            containerGrid.Children.Add(contentCanvas);
            
            // Set border content and add to the main grid
            border.Child = containerGrid;
            mainGrid.Children.Add(border);
            
            // Set the content of this control - filling the container
            this.Content = mainGrid;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            
            // Set up event handlers for zooming and panning
            contentCanvas.MouseWheel += ContentCanvas_MouseWheel;
            contentCanvas.MouseLeftButtonDown += ContentCanvas_MouseLeftButtonDown;
            contentCanvas.MouseMove += ContentCanvas_MouseMove;
            contentCanvas.MouseLeftButtonUp += ContentCanvas_MouseLeftButtonUp;
            
            // Subscribe to size change event to update canvas layout
            this.SizeChanged += ZoomViewer_SizeChanged;
        }

        /// <summary>
        /// Sets the zoom level to exactly 1:1 for pixel-perfect matching
        /// </summary>
        private void SetOneToOneZoom()
        {
            // Set zoom to exactly 1.0 for pixel-perfect display
            currentZoom = 1.0;
            zoomLevelText.Text = "1.0×";
            
            // Center view if needed
            if (parentWindow.LoadedImage != null)
            {
                // Center the current view on the image
                Point centerPoint = new Point(
                    parentWindow.LoadedImage.PixelWidth / 2,
                    parentWindow.LoadedImage.PixelHeight / 2
                );
                
                // Calculate new pan offset to center on this point
                panOffset = new Point(
                    centerPoint.X - (contentCanvas.ActualWidth / 2) / currentZoom,
                    centerPoint.Y - (contentCanvas.ActualHeight / 2) / currentZoom
                );
                
                UpdateZoomedContent();
            }
        }

        /// <summary>
        /// Handle size changed events to update content layout
        /// </summary>
        private void ZoomViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update content when the control resizes
            if (parentWindow.LoadedImage != null && this.Visibility == Visibility.Visible && contentInitialized)
            {
                UpdateZoomedContent();
            }
        }

        /// <summary>
        /// Updates the zoom viewer with the current main image
        /// </summary>
        public void UpdateContent()
        {
            if (parentWindow.LoadedImage != null)
            {
                // Set the image source
                zoomedImage.Source = parentWindow.LoadedImage;
                
                // Reset zoom and position
                ResetZoomAndPan();
                
                // Make the zoom viewer visible
                this.Visibility = Visibility.Visible;
                
                // Mark as initialized
                contentInitialized = true;
                
                // Update the content
                UpdateZoomedContent();
            }
            else
            {
                // Hide if no image is loaded
                this.Visibility = Visibility.Collapsed;
                contentInitialized = false;
            }
        }

        /// <summary>
        /// Updates the zoomed content based on current zoom level and pan offset
        /// </summary>
        private void UpdateZoomedContent()
        {
            if (parentWindow.LoadedImage == null || !contentInitialized)
                return;

            // Ensure canvas fills the available space
            if (contentCanvas.ActualWidth <= 0 || contentCanvas.ActualHeight <= 0)
            {
                // Wait for layout to complete if dimensions are not valid
                Dispatcher.BeginInvoke(new Action(() => UpdateZoomedContent()), 
                    System.Windows.Threading.DispatcherPriority.ContextIdle);
                return;
            }

            try
            {
                // Create a transform group for combined scaling and translation
                TransformGroup transformGroup = new TransformGroup();
                
                // Add scale transform
                ScaleTransform scaleTransform = new ScaleTransform(currentZoom, currentZoom);
                transformGroup.Children.Add(scaleTransform);
                
                // Add translation transform for panning
                TranslateTransform translateTransform = new TranslateTransform(
                    -panOffset.X * currentZoom, 
                    -panOffset.Y * currentZoom);
                transformGroup.Children.Add(translateTransform);
                
                // Apply the transform
                zoomedImage.RenderTransform = transformGroup;
                
                // Make sure image is sized correctly at its natural size
                // This is important for exact pixel matching
                zoomedImage.Width = double.NaN; // Auto
                zoomedImage.Height = double.NaN; // Auto
                
                // Update viewport indicator to show the visible area in the main view
                UpdateViewportIndicator();
                
                // Update markers within the zoom viewer
                UpdateMarkers();
            }
            catch (Exception ex)
            {
                // Handle any exceptions during rendering
                System.Diagnostics.Debug.WriteLine($"Error in UpdateZoomedContent: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the viewport indicator rectangle to show the current main view area
        /// </summary>
        private void UpdateViewportIndicator()
        {
            if (parentWindow.LoadedImage == null || parentWindow.DisplayImage == null)
                return;

            // Calculate main view scale using PixelWidth/PixelHeight for consistent scaling
            double mainScaleX = parentWindow.DisplayImage.Width / parentWindow.LoadedImage.PixelWidth;
            double mainScaleY = parentWindow.DisplayImage.Height / parentWindow.LoadedImage.PixelHeight;

            // Calculate visible area
            double visibleWidth = contentCanvas.ActualWidth / currentZoom;
            double visibleHeight = contentCanvas.ActualHeight / currentZoom;

            // Create a transform group for the viewport indicator
            TransformGroup transformGroup = new TransformGroup();
            
            // Add scale transform
            ScaleTransform scaleTransform = new ScaleTransform(currentZoom, currentZoom);
            transformGroup.Children.Add(scaleTransform);
            
            // Add translation transform for panning
            TranslateTransform translateTransform = new TranslateTransform(
                panOffset.X * currentZoom, 
                panOffset.Y * currentZoom);
            transformGroup.Children.Add(translateTransform);
            
            // Apply the transform
            viewportIndicator.RenderTransform = transformGroup;
            
            // Size the viewport indicator
            viewportIndicator.Width = visibleWidth;
            viewportIndicator.Height = visibleHeight;
            
            // Show the viewport indicator
            viewportIndicator.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Updates the markers within the zoom viewer to reflect the current zoom level
        /// using actual SpriteItemMarker instances like the main canvas
        /// </summary>
        private void UpdateMarkers()
        {
            // First, remove any existing markers from the zoom viewer
            foreach (var marker in zoomMarkers.Values)
            {
                contentCanvas.Children.Remove(marker);
            }
            zoomMarkers.Clear();

            // If no image is loaded, return
            if (parentWindow.LoadedImage == null)
                return;

            // Create new markers for each sprite using the same SpriteItemMarker class
            foreach (var mainMarkerPair in parentWindow.spriteMarkers)
            {
                // Get the sprite item and marker color
                var spriteItem = mainMarkerPair.Value.SpriteItem;
                var color = mainMarkerPair.Value.MarkerColor;
                
                // Create a new SpriteItemMarker for the zoom viewer
                var zoomMarker = new SpriteItemMarker(spriteItem, color);
                
                // We need to set a custom ScaleTransform to make the marker work with our zoom level
                TransformGroup markerTransform = new TransformGroup();
                
                // Apply our zoom level
                ScaleTransform markerScale = new ScaleTransform(currentZoom, currentZoom);
                markerTransform.Children.Add(markerScale);
                
                // Apply translation to account for panning
                TranslateTransform markerTranslate = new TranslateTransform(
                    -panOffset.X * currentZoom,
                    -panOffset.Y * currentZoom);
                markerTransform.Children.Add(markerTranslate);
                
                // Apply the transform directly to the marker
                zoomMarker.RenderTransform = markerTransform;
                zoomMarker.RenderTransformOrigin = new Point(0, 0);
                
                // Add the marker to the canvas
                contentCanvas.Children.Add(zoomMarker);
                
                // Keep track of it in our local dictionary
                zoomMarkers[spriteItem.Id] = zoomMarker;
                
                // Set appropriate z-index to make sure markers are above the image 
                // but below the viewport indicator
                Panel.SetZIndex(zoomMarker, 5);
                
                // Connect the zoom marker's selection event to update main window selection
                zoomMarker.MarkerSelected += (s, e) => {
                    // Forward the selection to the main window's data entry control
                    parentWindow.jsonDataEntry.SpriteCollection.SelectedItem = e;
                };
                
                // Update appearance based on selection state
                bool isSelected = spriteItem == parentWindow.jsonDataEntry.SpriteCollection.SelectedItem;
                zoomMarker.UpdateAppearance(isSelected);
            }

            // Make sure viewport indicator is at the front
            Panel.SetZIndex(viewportIndicator, 10);
        }

        /// <summary>
        /// Adjusts the zoom level by the specified step
        /// </summary>
        private void AdjustZoom(double zoomStep)
        {
            // Calculate new zoom level
            double newZoom = Math.Clamp(currentZoom + zoomStep, MIN_ZOOM, MAX_ZOOM);
            
            // Update zoom if changed
            if (Math.Abs(newZoom - currentZoom) > 0.01)
            {
                // Store the center point of the current view to maintain focus
                Point centerPoint = new Point(
                    panOffset.X + (contentCanvas.ActualWidth / 2) / currentZoom,
                    panOffset.Y + (contentCanvas.ActualHeight / 2) / currentZoom
                );
                
                // Update zoom level
                currentZoom = newZoom;
                zoomLevelText.Text = $"{currentZoom:F1}×";
                
                // Adjust pan offset to keep the center point focused
                panOffset = new Point(
                    centerPoint.X - (contentCanvas.ActualWidth / 2) / currentZoom,
                    centerPoint.Y - (contentCanvas.ActualHeight / 2) / currentZoom
                );
                
                // Update the zoomed content
                UpdateZoomedContent();
            }
        }

        /// <summary>
        /// Resets zoom to default and centers the view
        /// </summary>
        private void ResetZoom()
        {
            ResetZoomAndPan();
            zoomLevelText.Text = $"{currentZoom:F1}×";
        }

        /// <summary>
        /// Resets zoom level and pan offset to their default values
        /// </summary>
        private void ResetZoomAndPan()
        {
            // Use 1.0 for exact pixel matching as default
            currentZoom = DEFAULT_ZOOM;
            panOffset = new Point(0, 0);
            UpdateZoomedContent();
        }

        /// <summary>
        /// Handles mouse wheel events for zooming in and out
        /// </summary>
        private void ContentCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Store mouse position before zoom
            Point mousePos = e.GetPosition(contentCanvas);
            
            // Convert to image coordinates
            Point mouseImagePos = new Point(
                panOffset.X + mousePos.X / currentZoom,
                panOffset.Y + mousePos.Y / currentZoom
            );
            
            // Get zoom step based on wheel direction
            double step = e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP;
            
            // Calculate new zoom level
            double newZoom = Math.Clamp(currentZoom + step, MIN_ZOOM, MAX_ZOOM);
            
            // Only proceed if zoom actually changed
            if (Math.Abs(newZoom - currentZoom) > 0.01)
            {
                // Update zoom level
                currentZoom = newZoom;
                zoomLevelText.Text = $"{currentZoom:F1}×";
                
                // Calculate how to adjust pan offset to zoom toward mouse position
                panOffset = new Point(
                    mouseImagePos.X - mousePos.X / currentZoom,
                    mouseImagePos.Y - mousePos.Y / currentZoom
                );
                
                // Update the zoomed content
                UpdateZoomedContent();
            }
            
            // Mark event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles dragging the content for panning
        /// </summary>
        private void ContentCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start dragging
            isDragging = true;
            lastMousePos = e.GetPosition(contentCanvas);
            contentCanvas.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse movement for panning the zoomed view
        /// </summary>
        private void ContentCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // Get current mouse position
                Point currentMousePos = e.GetPosition(contentCanvas);
                
                // Calculate drag delta
                Vector delta = currentMousePos - lastMousePos;
                
                // Update pan offset - convert screen pixels to image coordinates
                panOffset = new Point(
                    panOffset.X - delta.X / currentZoom,
                    panOffset.Y - delta.Y / currentZoom
                );
                
                // Update content with new pan offset
                UpdateZoomedContent();
                
                // Store current position for next move
                lastMousePos = currentMousePos;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles end of panning operation
        /// </summary>
        private void ContentCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // End dragging
            if (isDragging)
            {
                isDragging = false;
                contentCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
    }
}