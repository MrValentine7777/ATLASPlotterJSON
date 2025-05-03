using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// A floating zoom viewer control that allows zooming into the atlas image
    /// while keeping the main view unchanged.
    /// </summary>
    public class ZoomViewer : Canvas
    {
        // Constants for the zoom viewer appearance and behavior
        private const double DEFAULT_WIDTH = 200.0;
        private const double DEFAULT_HEIGHT = 200.0;
        private const double MIN_ZOOM = 1.0;
        private const double MAX_ZOOM = 10.0;
        private const double ZOOM_STEP = 0.25;
        private const double BORDER_THICKNESS = 2.0;

        // Reference to the main window
        private readonly MainWindow parentWindow;

        // Zoom viewer components
        private readonly Border border;
        private readonly Canvas contentCanvas;
        private readonly Image zoomedImage;
        private readonly Rectangle viewportIndicator;
        private readonly StackPanel zoomControls;

        // Current zoom state
        private double currentZoom = 2.0;
        private Point panOffset = new Point(0, 0);
        private bool isDragging = false;
        private Point lastMousePos;

        // Data this marker represents
        private readonly SpriteItem spriteItem; // The sprite data model this marker represents
        private readonly Color markerColor;     // Color used for visual identification

        /// <summary>
        /// Gets the current zoom level of the zoom viewer
        /// </summary>
        public double CurrentZoom => currentZoom;

        /// <summary>
        /// Gets the color used for this marker
        /// </summary>
        public Color MarkerColor => markerColor;

        /// <summary>
        /// Creates a new zoom viewer attached to the parent window
        /// </summary>
        /// <param name="parent">The main window that owns this zoom viewer</param>
        public ZoomViewer(MainWindow parent)
        {
            parentWindow = parent;

            // Set a high Z-index to ensure the zoom viewer appears above all other elements
            Panel.SetZIndex(this, 1000);


            // Set up the main container with a border
            border = new Border
            {
                Width = DEFAULT_WIDTH,
                Height = DEFAULT_HEIGHT,
                Background = new SolidColorBrush(Color.FromArgb(225, 240, 240, 240)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(BORDER_THICKNESS),
                CornerRadius = new CornerRadius(3)
            };

            // Set up the canvas for the zoomed content
            contentCanvas = new Canvas
            {
                ClipToBounds = true,
                Width = DEFAULT_WIDTH - (BORDER_THICKNESS * 2),
                Height = DEFAULT_HEIGHT - (BORDER_THICKNESS * 2) - 24 // Leave room for controls
            };

            // Set up the zoomed image
            zoomedImage = new Image
            {
                Stretch = Stretch.None
            };

            // Set attached properties correctly
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
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Create zoom buttons
            Button zoomOutBtn = new Button { Content = "-", Width = 24, Padding = new Thickness(0) };
            TextBlock zoomLevelText = new TextBlock 
            { 
                Text = $"{currentZoom:F1}×", 
                VerticalAlignment = VerticalAlignment.Center,
                Width = 35,
                TextAlignment = TextAlignment.Center
            };
            Button zoomInBtn = new Button { Content = "+", Width = 24, Padding = new Thickness(0) };
            Button resetBtn = new Button { Content = "R", Width = 24, Padding = new Thickness(0) };

            // Add handlers for zoom buttons
            zoomOutBtn.Click += (s, e) => AdjustZoom(-ZOOM_STEP, zoomLevelText);
            zoomInBtn.Click += (s, e) => AdjustZoom(ZOOM_STEP, zoomLevelText);
            resetBtn.Click += (s, e) => ResetZoom(zoomLevelText);

            // Add controls to the zoom panel
            zoomControls.Children.Add(zoomOutBtn);
            zoomControls.Children.Add(zoomLevelText);
            zoomControls.Children.Add(zoomInBtn);
            zoomControls.Children.Add(resetBtn);

            // Add components to the canvas
            contentCanvas.Children.Add(zoomedImage);
            contentCanvas.Children.Add(viewportIndicator);

            // Add elements to the border
            Grid containerGrid = new Grid();
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            containerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            Grid.SetRow(contentCanvas, 0);
            Grid.SetRow(zoomControls, 1);
            
            containerGrid.Children.Add(contentCanvas);
            containerGrid.Children.Add(zoomControls);
            
            border.Child = containerGrid;

            // Add the border to this canvas
            this.Children.Add(border);

            // Set up event handlers for zooming and panning
            contentCanvas.MouseWheel += ContentCanvas_MouseWheel;
            contentCanvas.MouseLeftButtonDown += ContentCanvas_MouseLeftButtonDown;
            contentCanvas.MouseMove += ContentCanvas_MouseMove;
            contentCanvas.MouseLeftButtonUp += ContentCanvas_MouseLeftButtonUp;

            // Make the zoom viewer draggable
            border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
            
            // Position the zoom viewer in the lower left corner
            Canvas.SetLeft(this, 10);
            Canvas.SetBottom(this, 40); // Leave some space for the status bar

            // Initially hide until an image is loaded
            this.Visibility = Visibility.Collapsed;
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
                
                // Update the content
                UpdateZoomedContent();
            }
            else
            {
                // Hide if no image is loaded
                this.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the zoomed content based on current zoom level and pan offset
        /// </summary>
        private void UpdateZoomedContent()
        {
            if (parentWindow.LoadedImage == null)
                return;

            // Calculate the scale factor based on the zoom level
            double scaleFactor = currentZoom;
            
            // Set the size of the zoomed image
            zoomedImage.Width = parentWindow.LoadedImage.Width * scaleFactor;
            zoomedImage.Height = parentWindow.LoadedImage.Height * scaleFactor;
            
            // Position the image with pan offset
            Canvas.SetLeft(zoomedImage, -panOffset.X * scaleFactor);
            Canvas.SetTop(zoomedImage, -panOffset.Y * scaleFactor);
            
            // Update viewport indicator to show the visible area in the main view
            UpdateViewportIndicator();
            
            // Update markers within the zoom viewer
            UpdateMarkers();
        }

        /// <summary>
        /// Updates the viewport indicator rectangle to show the current main view area
        /// </summary>
        private void UpdateViewportIndicator()
        {
            if (parentWindow.LoadedImage == null || parentWindow.DisplayImage == null)
                return;

            // Calculate main view scale
            double mainScaleX = parentWindow.DisplayImage.Width / parentWindow.LoadedImage.Width;
            double mainScaleY = parentWindow.DisplayImage.Height / parentWindow.LoadedImage.Height;

            // Calculate visible area
            double visibleWidth = contentCanvas.ActualWidth / currentZoom;
            double visibleHeight = contentCanvas.ActualHeight / currentZoom;

            // Position and size the viewport indicator
            Canvas.SetLeft(viewportIndicator, (panOffset.X * currentZoom));
            Canvas.SetTop(viewportIndicator, (panOffset.Y * currentZoom));
            viewportIndicator.Width = visibleWidth * currentZoom;
            viewportIndicator.Height = visibleHeight * currentZoom;
            
            // Show the viewport indicator
            viewportIndicator.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Updates the markers within the zoom viewer to reflect the current zoom level
        /// </summary>
        private void UpdateMarkers()
        {
            // Remove any existing markers
            for (int i = contentCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (contentCanvas.Children[i] is Rectangle rect && 
                    rect != viewportIndicator)
                {
                    contentCanvas.Children.RemoveAt(i);
                }
            }

            // If no image is loaded, return
            if (parentWindow.LoadedImage == null)
                return;

            // Add sprite markers to the zoomed view
            foreach (var markerPair in parentWindow.spriteMarkers)
            {
                var marker = markerPair.Value;
                
                // Get sprite position and dimensions
                double x = marker.SpriteItem.Source.X;
                double y = marker.SpriteItem.Source.Y;
                double width = marker.SpriteItem.Source.Width;
                double height = marker.SpriteItem.Source.Height;
                
                // Create a new rectangle for the marker
                var rect = new Rectangle
                {
                    Width = width * currentZoom,
                    Height = height * currentZoom,
                    Stroke = new SolidColorBrush(marker.MarkerColor),
                    StrokeThickness = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(30, 
                        marker.MarkerColor.R, 
                        marker.MarkerColor.G, 
                        marker.MarkerColor.B))
                };

                // Position the marker in the zoomed view
                Canvas.SetLeft(rect, (x - panOffset.X) * currentZoom);
                Canvas.SetTop(rect, (y - panOffset.Y) * currentZoom);
                
                // Add to the canvas (under the viewport indicator)
                contentCanvas.Children.Insert(contentCanvas.Children.IndexOf(viewportIndicator), rect);
            }
        }

        /// <summary>
        /// Adjusts the zoom level by the specified step
        /// </summary>
        private void AdjustZoom(double zoomStep, TextBlock zoomText)
        {
            // Calculate new zoom level
            double newZoom = Math.Clamp(currentZoom + zoomStep, MIN_ZOOM, MAX_ZOOM);
            
            // Update zoom if changed
            if (Math.Abs(newZoom - currentZoom) > 0.01)
            {
                currentZoom = newZoom;
                zoomText.Text = $"{currentZoom:F1}×";
                
                // Update the zoomed content
                UpdateZoomedContent();
            }
        }

        /// <summary>
        /// Resets zoom to default and centers the view
        /// </summary>
        private void ResetZoom(TextBlock zoomText)
        {
            ResetZoomAndPan();
            zoomText.Text = $"{currentZoom:F1}×";
        }

        /// <summary>
        /// Resets zoom level and pan offset to their default values
        /// </summary>
        private void ResetZoomAndPan()
        {
            currentZoom = 2.0;
            panOffset = new Point(0, 0);
            UpdateZoomedContent();
        }

        /// <summary>
        /// Handles mouse wheel events for zooming in and out
        /// </summary>
        private void ContentCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Get zoom step based on wheel direction
            double step = e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP;
            
            // Get the zoom text block for updating
            TextBlock zoomText = zoomControls.Children[1] as TextBlock;
            
            // Adjust zoom
            AdjustZoom(step, zoomText);
            
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
                
                // Update pan offset
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

        /// <summary>
        /// Allows the zoom viewer itself to be dragged around
        /// </summary>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Capture the mouse to drag the entire zoom viewer
            if (e.OriginalSource == border)
            {
                lastMousePos = e.GetPosition(parentWindow);
                border.CaptureMouse();
                border.MouseMove += Border_MouseMove;
                border.MouseLeftButtonUp += Border_MouseLeftButtonUp;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Moves the zoom viewer with the mouse
        /// </summary>
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (border.IsMouseCaptured)
            {
                // Calculate new position
                Point currentPos = e.GetPosition(parentWindow);
                Vector delta = currentPos - lastMousePos;
                
                double newLeft = Canvas.GetLeft(this) + delta.X;
                double newTop = Canvas.GetTop(this) + delta.Y;
                
                // Ensure the zoom viewer stays within the window bounds
                newLeft = Math.Max(0, Math.Min(newLeft, parentWindow.ActualWidth - border.ActualWidth));
                newTop = Math.Max(0, Math.Min(newTop, parentWindow.ActualHeight - border.ActualHeight));
                
                // Update position
                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);
                Canvas.SetBottom(this, double.NaN); // Clear bottom property
                
                // Update last position
                lastMousePos = currentPos;
            }
        }

        /// <summary>
        /// Ends the dragging of the zoom viewer
        /// </summary>
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Release mouse capture
            if (border.IsMouseCaptured)
            {
                border.ReleaseMouseCapture();
                border.MouseMove -= Border_MouseMove;
                border.MouseLeftButtonUp -= Border_MouseLeftButtonUp;
            }
        }
    }
}