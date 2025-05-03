using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Threading;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// A docked zoom viewer control that allows zooming into the atlas image
    /// while keeping the main view unchanged.
    /// </summary>
    public class ZoomViewer : UserControl
    {
        // Constants for the zoom viewer appearance and behavior
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

        // Pixel grid overlay
        private readonly Canvas gridCanvas;
        private bool showPixelGrid = false;
        private readonly CheckBox gridCheckbox;

        // Current zoom state
        private double currentZoom = DEFAULT_ZOOM; // Initialize to 1.0 for pixel-perfect match
        private Point panOffset = new Point(0, 0);
        private bool isDragging = false;
        private Point lastMousePos;

        // Track whether content is initialized
        private bool contentInitialized = false;

        // Combined image with markers
        private WriteableBitmap combinedImageBitmap;

        // Timer for delayed refresh
        private readonly DispatcherTimer refreshTimer;

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

            // Create grid canvas overlay for pixel grid
            gridCanvas = new Canvas
            {
                ClipToBounds = true,
                Background = Brushes.Transparent,
                IsHitTestVisible = false // Don't intercept mouse events
            };
            Panel.SetZIndex(gridCanvas, 5); // Ensure grid is above image but below UI elements

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
            Button oneToOneBtn = new Button
            {
                Content = "1:1",
                Width = 30,
                Padding = new Thickness(0),
                ToolTip = "Set to exact 1:1 pixel match"
            };

            // Add refresh button for updating the captured image
            Button refreshBtn = new Button
            {
                Content = "↻",
                Width = 24,
                Padding = new Thickness(0),
                ToolTip = "Refresh zoom view"
            };

            // Add grid checkbox for toggling pixel grid
            gridCheckbox = new CheckBox
            {
                Content = "Grid",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                ToolTip = "Show pixel grid",
                IsChecked = showPixelGrid // Initialize checkbox state to match variable
            };
            gridCheckbox.Checked += (s, e) => TogglePixelGrid(true);
            gridCheckbox.Unchecked += (s, e) => TogglePixelGrid(false);

            // Add handlers for zoom buttons
            zoomOutBtn.Click += (s, e) => AdjustZoom(-ZOOM_STEP);
            zoomInBtn.Click += (s, e) => AdjustZoom(ZOOM_STEP);
            resetBtn.Click += (s, e) => ResetZoom();
            oneToOneBtn.Click += (s, e) => SetOneToOneZoom();
            refreshBtn.Click += (s, e) => UpdateCombinedImage();

            // Add controls to the zoom panel
            zoomControls.Children.Add(zoomOutBtn);
            zoomControls.Children.Add(zoomLevelText);
            zoomControls.Children.Add(zoomInBtn);
            zoomControls.Children.Add(oneToOneBtn); // Add the new 1:1 button
            zoomControls.Children.Add(resetBtn);
            zoomControls.Children.Add(refreshBtn);  // Add refresh button
            zoomControls.Children.Add(gridCheckbox); // Add grid checkbox

            // Add components to the canvas
            contentCanvas.Children.Add(zoomedImage);
            contentCanvas.Children.Add(gridCanvas); // Add grid canvas overlay
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

            // Subscribe to parent window's event for marker updates
            if (parentWindow != null)
            {
                parentWindow.MarkersUpdated += ParentWindow_MarkersUpdated;
            }

            // Initialize refresh timer
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            refreshTimer.Tick += (s, e) =>
            {
                refreshTimer.Stop();
                UpdateCombinedImage();
            };

            // Add visibility change handler
            this.IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue && contentInitialized && showPixelGrid && gridCheckbox.IsChecked == true)
                {
                    DrawPixelGrid();
                }
            };
        }

        /// <summary>
        /// Toggles the pixel grid display on or off
        /// </summary>
        /// <param name="show">True to show the grid, false to hide it</param>
        private void TogglePixelGrid(bool show)
        {
            showPixelGrid = show;

            if (show)
            {
                // If we're already zoomed in enough, draw the grid immediately
                if (currentZoom >= 2.0) // Match the new threshold
                {
                    DrawPixelGrid();
                }
                else
                {
                    // If zoom level is too low, inform user with a tooltip
                    ToolTip tt = new ToolTip();
                    tt.Content = "Increase zoom level to see the grid (2.0x or higher)";
                    tt.IsOpen = true;
                    tt.StaysOpen = false;
                    gridCheckbox.ToolTip = tt;

                    // Clear in case there was anything
                    gridCanvas.Children.Clear();
                }
            }
            else
            {
                gridCanvas.Children.Clear();
            }
        }

        /// <summary>
        /// Draws the pixel grid overlay based on the current zoom level and pan offset
        /// </summary>
        private void DrawPixelGrid()
        {
            if (!showPixelGrid || combinedImageBitmap == null)
            {
                return;
            }

            // Clear existing grid
            gridCanvas.Children.Clear();

            // Lower the threshold to make grid visible at lower zoom levels
            // This prevents UI slowdown when zoomed out too far
            if (currentZoom < 2.0) // Changed from 3.0 to 2.0
            {
                return;
            }

            // Get the visible area in image coordinates
            double visibleStartX = panOffset.X;
            double visibleStartY = panOffset.Y;
            double visibleEndX = visibleStartX + contentCanvas.ActualWidth / currentZoom;
            double visibleEndY = visibleStartY + contentCanvas.ActualHeight / currentZoom;

            // Convert to integer pixel boundaries
            int startX = Math.Max(0, (int)Math.Floor(visibleStartX));
            int startY = Math.Max(0, (int)Math.Floor(visibleStartY));
            int endX = Math.Min(combinedImageBitmap.PixelWidth, (int)Math.Ceiling(visibleEndX));
            int endY = Math.Min(combinedImageBitmap.PixelHeight, (int)Math.Ceiling(visibleEndY));

            // Create thin magenta grid pen with 0.5 thickness
            Pen gridPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 0, 255)), 0.5);

            // Create transform for the grid lines to match image position and zoom
            TransformGroup transformGroup = new TransformGroup();
            ScaleTransform scaleTransform = new ScaleTransform(currentZoom, currentZoom);
            transformGroup.Children.Add(scaleTransform);
            TranslateTransform translateTransform = new TranslateTransform(
                -panOffset.X * currentZoom,
                -panOffset.Y * currentZoom);
            transformGroup.Children.Add(translateTransform);

            // Draw vertical grid lines
            for (int x = startX; x <= endX; x++)
            {
                Line line = new Line
                {
                    X1 = x,
                    Y1 = startY,
                    X2 = x,
                    Y2 = endY,
                    Stroke = gridPen.Brush,
                    StrokeThickness = gridPen.Thickness / currentZoom, // Adjust for zoom to keep line thin
                    RenderTransform = transformGroup
                };
                gridCanvas.Children.Add(line);
            }

            // Draw horizontal grid lines
            for (int y = startY; y <= endY; y++)
            {
                Line line = new Line
                {
                    X1 = startX,
                    Y1 = y,
                    X2 = endX,
                    Y2 = y,
                    Stroke = gridPen.Brush,
                    StrokeThickness = gridPen.Thickness / currentZoom, // Adjust for zoom to keep line thin
                    RenderTransform = transformGroup
                };
                gridCanvas.Children.Add(line);
            }
        }

        /// <summary>
        /// Event handler for when markers are updated in the main window
        /// Schedules a refresh with a small delay to ensure UI is updated
        /// </summary>
        private void ParentWindow_MarkersUpdated(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible && contentInitialized)
            {
                // Use timer to prevent multiple refreshes when rapidly updating markers
                refreshTimer.Stop();
                refreshTimer.Start();
            }
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
            if (combinedImageBitmap != null)
            {
                // Center the current view on the image
                Point centerPoint = new Point(
                    combinedImageBitmap.PixelWidth / 2,
                    combinedImageBitmap.PixelHeight / 2
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
            if (combinedImageBitmap != null && this.Visibility == Visibility.Visible && contentInitialized)
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
                // Create combined image from the main canvas
                UpdateCombinedImage();

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
        /// Captures the main canvas content (image and markers) into a single bitmap
        /// </summary>
        public void UpdateCombinedImage()
        {
            if (parentWindow?.LoadedImage == null)
                return;

            try
            {
                // Get the main canvas from parent window
                var mainCanvas = parentWindow.MainImageCanvas;
                if (mainCanvas == null)
                    return;

                // Get the size of the original image
                int width = parentWindow.LoadedImage.PixelWidth;
                int height = parentWindow.LoadedImage.PixelHeight;

                // Create a RenderTargetBitmap to render the entire canvas
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    width, height, 96, 96, PixelFormats.Pbgra32);

                // Create a DrawingVisual to render the image and markers at original scale
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    // First draw the background (original image)
                    drawingContext.DrawImage(parentWindow.LoadedImage, new Rect(0, 0, width, height));

                    // Then draw each marker at its correct position
                    foreach (var markerPair in parentWindow.spriteMarkers)
                    {
                        var marker = markerPair.Value;
                        var spriteItem = marker.SpriteItem;

                        // Draw a rectangle for the sprite
                        Rect spriteRect = new Rect(
                            spriteItem.Source.X,
                            spriteItem.Source.Y,
                            spriteItem.Source.Width,
                            spriteItem.Source.Height);

                        // Get color and brush for this marker
                        Color color = marker.MarkerColor;
                        SolidColorBrush fillBrush = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B));
                        Pen strokePen = new Pen(new SolidColorBrush(color), 2);

                        // If sprite is selected, use a dashed stroke
                        if (spriteItem == parentWindow.jsonDataEntry.SpriteCollection.SelectedItem)
                        {
                            strokePen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
                            strokePen.Thickness = 3;
                        }

                        // Draw the sprite rectangle
                        drawingContext.DrawRectangle(fillBrush, strokePen, spriteRect);

                        // Draw the sprite name text
                        FormattedText formattedText = new FormattedText(
                            $"#{spriteItem.Id}: {spriteItem.Name}",
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            12,
                            new SolidColorBrush(color),
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);

                        // Draw the text background
                        Rect textRect = new Rect(
                            spriteItem.Source.X,
                            spriteItem.Source.Y - formattedText.Height - 2,
                            formattedText.Width + 8,
                            formattedText.Height + 4);

                        // If sprite is too close to the top, draw the label below it
                        if (textRect.Y < 0)
                        {
                            textRect.Y = spriteItem.Source.Y + spriteItem.Source.Height + 2;
                        }

                        // Draw text background
                        drawingContext.DrawRectangle(
                            new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                            null,
                            textRect);

                        // Draw the text
                        drawingContext.DrawText(formattedText, new Point(
                            textRect.X + 4,
                            textRect.Y + 2));
                    }
                }

                // Render the visual to the bitmap
                renderBitmap.Render(drawingVisual);

                // Store the bitmap
                combinedImageBitmap = new WriteableBitmap(renderBitmap);

                // Set the image source
                zoomedImage.Source = combinedImageBitmap;

                // Force layout update
                UpdateZoomedContent();
            }
            catch (Exception ex)
            {
                // Log any errors
                System.Diagnostics.Debug.WriteLine($"Error capturing combined image: {ex.Message}");

                // Fallback to just showing the original image without markers
                if (parentWindow.LoadedImage != null)
                {
                    zoomedImage.Source = parentWindow.LoadedImage;
                }
            }

            // Update grid if it's enabled
            if (showPixelGrid)
            {
                DrawPixelGrid();
            }
        }

        /// <summary>
        /// Updates the zoomed content based on current zoom level and pan offset
        /// </summary>
        private void UpdateZoomedContent()
        {
            if (combinedImageBitmap == null || !contentInitialized)
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

                // Apply the transform to the image
                zoomedImage.RenderTransform = transformGroup;

                // Make sure image is sized correctly at its natural size
                // This is important for exact pixel matching
                zoomedImage.Width = double.NaN; // Auto
                zoomedImage.Height = double.NaN; // Auto

                // Apply the transform to the grid canvas
                gridCanvas.RenderTransform = transformGroup;

                // Update viewport indicator to show the visible area in the main view
                UpdateViewportIndicator();

                // Update the pixel grid if enabled
                if (showPixelGrid)
                {
                    DrawPixelGrid();
                }
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
            if (combinedImageBitmap == null || parentWindow.DisplayImage == null)
                return;

            // Calculate main view scale using PixelWidth/PixelHeight for consistent scaling
            double mainScaleX = parentWindow.DisplayImage.Width / combinedImageBitmap.PixelWidth;
            double mainScaleY = parentWindow.DisplayImage.Height / combinedImageBitmap.PixelHeight;

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