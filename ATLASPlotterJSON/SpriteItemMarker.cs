using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace ATLASPlotterJSON
{
    public class SpriteItemMarker : Canvas
    {
        private readonly Rectangle sourceBox;
        private readonly TextBlock label;
        private readonly SpriteItem spriteItem;
        private readonly Color markerColor;
        
        public SpriteItem SpriteItem => spriteItem;
        
        public event EventHandler<SpriteItem> MarkerSelected;
        
        public SpriteItemMarker(SpriteItem item, Color color)
        {
            spriteItem = item;
            markerColor = color;
            
            // Create a rectangle to visualize the sprite source area
            sourceBox = new Rectangle
            {
                Width = item.Source.Width,
                Height = item.Source.Height,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B))
            };
            
            // Create label to show ID and name
            label = new TextBlock
            {
                Text = $"#{item.Id}: {item.Name}",
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Foreground = new SolidColorBrush(color),
                Padding = new Thickness(4),
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            
            // Add elements to this canvas
            Children.Add(sourceBox);
            Children.Add(label);
            
            // Position elements
            UpdatePosition();
            
            // Add mouse handling for selection
            this.MouseLeftButtonDown += SpriteItemMarker_MouseLeftButtonDown;
            sourceBox.MouseLeftButtonDown += SpriteItemMarker_MouseLeftButtonDown;
            label.MouseLeftButtonDown += SpriteItemMarker_MouseLeftButtonDown;
            
            // Set ZIndex to ensure it's above the image but below UI elements
            SetZIndex(this, 50);
            
            // Subscribe to property changes of the sprite item
            spriteItem.PropertyChanged += SpriteItem_PropertyChanged;
        }
        
        private void SpriteItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update the visual display when properties change
            if (e.PropertyName == "Name")
            {
                UpdateNameDisplay();
            }
            else if (e.PropertyName == "Source" || 
                     e.PropertyName == "X" || 
                     e.PropertyName == "Y" || 
                     e.PropertyName == "Width" || 
                     e.PropertyName == "Height")
            {
                UpdatePosition();
            }
        }
        
        private void UpdateNameDisplay()
        {
            // Update the label text to reflect the current name
            label.Text = $"#{spriteItem.Id}: {spriteItem.Name}";
        }
        
        public void UpdatePosition(double zoomLevel = 1.0)
        {
            // Position and size the source box
            Canvas.SetLeft(sourceBox, spriteItem.Source.X);
            Canvas.SetTop(sourceBox, spriteItem.Source.Y);
            sourceBox.Width = spriteItem.Source.Width;
            sourceBox.Height = spriteItem.Source.Height;
            sourceBox.StrokeThickness = 2 / zoomLevel;
            
            // Update the label text to ensure it's current
            UpdateNameDisplay();
            
            // Position the label at the top-left corner of the box
            Canvas.SetLeft(label, spriteItem.Source.X);
            Canvas.SetTop(label, spriteItem.Source.Y - label.ActualHeight - 2);
            
            // Adjust text for zoom
            label.FontSize = 12 / zoomLevel;
            label.Padding = new Thickness(4 / zoomLevel);
        }
        
        public void UpdateAppearance(bool isSelected)
        {
            // Change appearance based on selection state
            if (isSelected)
            {
                sourceBox.StrokeThickness = 3;
                sourceBox.StrokeDashArray = new DoubleCollection { 4, 2 };
                label.FontWeight = FontWeights.ExtraBold;
            }
            else
            {
                sourceBox.StrokeThickness = 2;
                sourceBox.StrokeDashArray = null;
                label.FontWeight = FontWeights.Bold;
            }
        }
        
        private void SpriteItemMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Notify listeners that this marker has been selected
            MarkerSelected?.Invoke(this, spriteItem);
            e.Handled = true;
        }
    }
}