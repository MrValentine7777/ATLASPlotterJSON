using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// Displays and tracks the current pixel location in the sprite atlas editor.
    /// This visual component shows a highlight box and coordinate text to help users
    /// precisely locate pixels on the sprite atlas image.
    /// </summary>
    public class PixelLocationDisplay : Canvas
    {
        // UI elements that make up the display
        private readonly TextBlock coordsText;      // Text displaying X,Y coordinates
        private readonly Rectangle highlightBox;    // Box highlighting the current pixel
        private readonly MainWindow parentWindow;   // Reference to main window for context
        
        // Size of the highlight box in screen space
        // This determines how large the red selection box appears
        private const double BoxSize = 12.0;
        
        // Additional vertical offset to avoid overlap with selection name
        // Moves the text label further up to prevent overlapping with sprite names
        private const double VerticalOffset = 10.0;
        
        /// <summary>
        /// Gets the current pixel location being tracked
        /// </summary>
        public Point CurrentLocation { get; private set; }
        
        /// <summary>
        /// Creates a new pixel location display attached to the parent window
        /// </summary>
        /// <param name="parent">The main window that owns this display</param>
        public PixelLocationDisplay(MainWindow parent)
        {
            // Store reference to the parent window for later use
            parentWindow = parent;
            
            // Create a highlight box to show the current pixel
            // This box visually indicates which pixel is currently selected
            highlightBox = new Rectangle
            {
                Width = BoxSize,                                         // Default width
                Height = BoxSize,                                        // Default height
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)), // Semi-transparent red fill
                Stroke = Brushes.Red,                                    // Red outline
                StrokeThickness = 2                                      // Outline thickness
            };
            
            // Create text overlay for displaying the coordinates
            // This shows the exact X,Y position to the user
            coordsText = new TextBlock
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), // Semi-transparent black background
                Foreground = Brushes.White,                              // White text for readability
                Padding = new Thickness(4),                              // Padding inside the text box
                FontWeight = FontWeights.Bold                            // Bold text for better visibility
            };
            
            // Add the UI elements to this canvas in the correct z-order
            this.Children.Add(highlightBox);
            this.Children.Add(coordsText);
            
            // Initialize to a default position (0,0)
            UpdatePosition(new Point(0, 0));
        }
        
        /// <summary>
        /// Updates the position of the pixel location display to track a new location
        /// </summary>
        /// <param name="location">The new pixel coordinate to display</param>
        /// <param name="zoomLevel">Current zoom level of the canvas (affects visual size)</param>
        public void UpdatePosition(Point location, double zoomLevel = 1.0)
        {
            // Store the current location for future reference
            CurrentLocation = location;
            
            // Position the highlight box centered on the target pixel
            // Subtract half the box size to center it on the pixel coordinates
            Canvas.SetLeft(highlightBox, location.X - BoxSize / 2);
            Canvas.SetTop(highlightBox, location.Y - BoxSize / 2);
            
            // Adjust box size and stroke thickness for zoom level
            // When zoomed in, the box should appear smaller relative to the pixels
            highlightBox.Width = highlightBox.Height = BoxSize / zoomLevel;
            highlightBox.StrokeThickness = 2 / zoomLevel;
            
            // Update the text to show current X,Y coordinates
            // Cast to integer since we're working with discrete pixels
            coordsText.Text = $"X: {(int)location.X}, Y: {(int)location.Y}";
            
            // Position the text near the highlight box but ensure it's visible
            // The text is positioned to the right and above the highlight box
            Canvas.SetLeft(coordsText, location.X + (BoxSize / zoomLevel));
            Canvas.SetTop(coordsText, location.Y - coordsText.ActualHeight - (BoxSize / zoomLevel) - (VerticalOffset / zoomLevel));
            
            // Adjust text size and padding for current zoom level
            // This ensures the text remains readable at all zoom levels
            coordsText.FontSize = 12 / zoomLevel;
            coordsText.Padding = new Thickness(4 / zoomLevel);
        }
        
        /// <summary>
        /// Makes the pixel location display visible
        /// </summary>
        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// Hides the pixel location display
        /// </summary>
        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}