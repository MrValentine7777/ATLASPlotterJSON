using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ATLASPlotterJSON
{
    public class PixelLocationDisplay : Canvas
    {
        private readonly TextBlock coordsText;
        private readonly Rectangle highlightBox;
        private readonly MainWindow parentWindow;
        
        // Size of the highlight box in screen space
        private const double BoxSize = 12.0;
        
        public Point CurrentLocation { get; private set; }
        
        public PixelLocationDisplay(MainWindow parent)
        {
            parentWindow = parent;
            
            // Create a highlight box to show the current pixel
            highlightBox = new Rectangle
            {
                Width = BoxSize,
                Height = BoxSize,
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)),
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            
            // Create text overlay for coordinates
            coordsText = new TextBlock
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Foreground = Brushes.White,
                Padding = new Thickness(4),
                FontWeight = FontWeights.Bold
            };
            
            // Add elements to this canvas
            this.Children.Add(highlightBox);
            this.Children.Add(coordsText);
            
            // Initialize position
            UpdatePosition(new Point(0, 0));
        }
        
        public void UpdatePosition(Point location, double zoomLevel = 1.0)
        {
            CurrentLocation = location;
            
            // Position the highlight box
            Canvas.SetLeft(highlightBox, location.X - BoxSize / 2);
            Canvas.SetTop(highlightBox, location.Y - BoxSize / 2);
            
            // Adjust box size and stroke thickness for zoom
            highlightBox.Width = highlightBox.Height = BoxSize / zoomLevel;
            highlightBox.StrokeThickness = 2 / zoomLevel;
            
            // Update the text
            coordsText.Text = $"X: {(int)location.X}, Y: {(int)location.Y}";
            
            // Position the text near the highlight box but ensure it's visible
            Canvas.SetLeft(coordsText, location.X + (BoxSize / zoomLevel));
            Canvas.SetTop(coordsText, location.Y - coordsText.ActualHeight - (BoxSize / zoomLevel));
            
            // Adjust text for zoom
            coordsText.FontSize = 12 / zoomLevel;
            coordsText.Padding = new Thickness(4 / zoomLevel);
        }
        
        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }
        
        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}