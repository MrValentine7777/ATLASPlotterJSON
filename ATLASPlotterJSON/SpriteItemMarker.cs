using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// Visual representation of a sprite item on the sprite atlas canvas.
    /// This class creates and manages the visual elements that show where a sprite is 
    /// located within the atlas image, including a colored rectangle and text label.
    /// </summary>
    /// <remarks>
    /// Each SpriteItem has a corresponding SpriteItemMarker which is displayed on top of 
    /// the sprite atlas image. The marker shows the sprite's boundaries and name,
    /// and handles user interactions like selection.
    /// </remarks>
    public class SpriteItemMarker : Canvas
    {
        // Visual elements that make up the marker
        private readonly Rectangle sourceBox;   // Rectangle outlining the sprite boundaries
        private readonly TextBlock label;       // Text showing sprite ID and name
        
        // Data this marker represents
        private readonly SpriteItem spriteItem; // The sprite data model this marker represents
        private readonly Color markerColor;     // Color used for visual identification
        
        /// <summary>
        /// Gets the sprite item associated with this marker
        /// </summary>
        public SpriteItem SpriteItem => spriteItem;
        
        /// <summary>
        /// Event that fires when the user selects this marker.
        /// The MainWindow subscribes to this event to know when a marker is selected.
        /// </summary>
        public event EventHandler<SpriteItem> MarkerSelected;
        
        /// <summary>
        /// Creates a new sprite marker to visually represent a sprite item on the atlas
        /// </summary>
        /// <param name="item">The sprite item data to represent</param>
        /// <param name="color">The color to use for this marker (unique to each sprite)</param>
        /// <remarks>
        /// This is created by MainWindow.CreateSpriteMarker() when sprites are loaded
        /// or added through the JsonDataEntryControl
        /// </remarks>
        public SpriteItemMarker(SpriteItem item, Color color)
        {
            // Store references to the data
            spriteItem = item;
            markerColor = color;
            
            // Create a rectangle to visualize the sprite source area (where it is in the atlas)
            // This rectangle shows exactly which pixels in the atlas image belong to this sprite
            sourceBox = new Rectangle
            {
                Width = item.Source.Width,                                        // Width from the sprite data
                Height = item.Source.Height,                                      // Height from the sprite data
                Stroke = new SolidColorBrush(color),                              // Outline using the sprite's color
                StrokeThickness = 2,                                              // Border thickness
                Fill = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B))  // Semi-transparent fill
            };
            
            // Create label to show ID and name above the sprite
            // This helps identify the sprite when multiple sprites are visible
            label = new TextBlock
            {
                Text = $"#{item.Id}: {item.Name}",                              // Format: "#ID: Name"
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), // Semi-transparent black background
                Foreground = new SolidColorBrush(color),                        // Text color matches sprite color
                Padding = new Thickness(4),                                     // Padding inside the label
                FontWeight = FontWeights.Bold,                                  // Bold text for readability
                FontSize = 12                                                   // Default font size
            };
            
            // Add visual elements to this canvas
            // The Canvas class (which SpriteItemMarker inherits from) can contain multiple children
            Children.Add(sourceBox);  // Add the rectangle first (below the text)
            Children.Add(label);      // Add the label second (above the rectangle)
            
            // Position elements according to sprite data
            // This places the rectangle and label at the correct position on the canvas
            UpdatePosition();
            
            // Add mouse handling for selection
            // These event handlers allow the user to select this sprite by clicking on it
            this.MouseLeftButtonDown += SpriteItemMarker_MouseLeftButtonDown;      // Clicking the marker background
            sourceBox.MouseLeftButtonDown += SpriteItemMarker_MouseLeftButtonDown; // Clicking the rectangle
            label.MouseLeftButtonDown += SpriteItemMarker_MouseLeftButtonDown;     // Clicking the label
            
            // Set ZIndex to ensure it's above the image but below UI elements
            // This makes sure the marker is visible but doesn't interfere with UI controls
            SetZIndex(this, 50);
            
            // Subscribe to property changes of the sprite item
            // When the sprite data changes, we need to update our visual representation
            spriteItem.PropertyChanged += SpriteItem_PropertyChanged;
        }
        
        /// <summary>
        /// Handles property changes in the underlying sprite item
        /// </summary>
        /// <param name="sender">The sprite item that changed</param>
        /// <param name="e">Information about which property changed</param>
        /// <remarks>
        /// This ensures the visual marker stays in sync with the sprite data.
        /// When properties like position or name change in the JsonDataEntryControl,
        /// this method updates the visual representation accordingly.
        /// </remarks>
        private void SpriteItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update the visual display when properties change
            if (e.PropertyName == "Name")
            {
                // If the name changed, update just the label text
                UpdateNameDisplay();
            }
            else if (e.PropertyName == "Source" || 
                     e.PropertyName == "X" || 
                     e.PropertyName == "Y" || 
                     e.PropertyName == "Width" || 
                     e.PropertyName == "Height")
            {
                // If position or size changed, update the entire marker position
                UpdatePosition();
            }
        }
        
        /// <summary>
        /// Updates the displayed name label to match the current sprite name
        /// </summary>
        /// <remarks>
        /// Called when the user edits the sprite name in the JsonDataEntryControl
        /// </remarks>
        private void UpdateNameDisplay()
        {
            // Update the label text to reflect the current name
            // Format is "#ID: Name" for clear identification
            label.Text = $"#{spriteItem.Id}: {spriteItem.Name}";
        }
        
        /// <summary>
        /// Updates the position and size of the marker to match the sprite data
        /// </summary>
        /// <param name="zoomLevel">Current zoom level of the canvas (default is 1.0)</param>
        /// <remarks>
        /// Called when:
        /// 1. The sprite position or size changes in the JsonDataEntryControl
        /// 2. The user moves the sprite by clicking on the canvas
        /// 3. The zoom level changes
        /// </remarks>
        public void UpdatePosition(double zoomLevel = 1.0)
        {
            // Position and size the source box to match the sprite data
            // This places the rectangle at the exact position of the sprite in the atlas
            Canvas.SetLeft(sourceBox, spriteItem.Source.X);
            Canvas.SetTop(sourceBox, spriteItem.Source.Y);
            sourceBox.Width = spriteItem.Source.Width;
            sourceBox.Height = spriteItem.Source.Height;
            
            // Adjust border thickness for current zoom level
            // Makes the border appear consistent regardless of zoom
            sourceBox.StrokeThickness = 2 / zoomLevel;
            
            // Update the label text to ensure it's current
            UpdateNameDisplay();
            
            // Position the label at the top-left corner of the box
            // The label appears just above the sprite rectangle
            Canvas.SetLeft(label, spriteItem.Source.X);
            Canvas.SetTop(label, spriteItem.Source.Y - label.ActualHeight - 2);
            
            // Adjust text for zoom level
            // Makes text readable at any zoom level
            label.FontSize = 12 / zoomLevel;
            label.Padding = new Thickness(4 / zoomLevel);
        }
        
        /// <summary>
        /// Updates the visual appearance based on whether the sprite is selected
        /// </summary>
        /// <param name="isSelected">True if this sprite is currently selected, false otherwise</param>
        /// <remarks>
        /// Called by MainWindow.OnSelectedSpriteChanged when the selected sprite changes.
        /// This provides visual feedback to show which sprite is currently selected.
        /// </remarks>
        public void UpdateAppearance(bool isSelected)
        {
            // Change appearance based on selection state
            if (isSelected)
            {
                // Selected sprite has thicker, dashed border and bolder text
                sourceBox.StrokeThickness = 3;
                sourceBox.StrokeDashArray = new DoubleCollection { 4, 2 };  // Dashed line pattern
                label.FontWeight = FontWeights.ExtraBold;                   // Extra bold text
            }
            else
            {
                // Non-selected sprite has regular border and normal bold text
                sourceBox.StrokeThickness = 2;
                sourceBox.StrokeDashArray = null;                           // Solid line
                label.FontWeight = FontWeights.Bold;                        // Regular bold text
            }
        }
        
        /// <summary>
        /// Handles mouse clicks on the marker and notifies listeners about selection
        /// </summary>
        /// <param name="sender">The object that was clicked</param>
        /// <param name="e">Mouse event information</param>
        /// <remarks>
        /// When a user clicks on any part of this marker (label, rectangle, or background),
        /// this method fires the MarkerSelected event which is handled by MainWindow.Marker_Selected
        /// to update the currently selected sprite.
        /// </remarks>
        private void SpriteItemMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Notify listeners (MainWindow) that this marker has been selected
            // This will trigger MainWindow.Marker_Selected which updates jsonDataEntry.SpriteCollection.SelectedItem
            MarkerSelected?.Invoke(this, spriteItem);
            e.Handled = true;  // Mark the event as handled so it doesn't bubble up
        }
    }
}