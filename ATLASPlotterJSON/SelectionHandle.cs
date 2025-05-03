using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// Defines the four possible positions for resize handles on a selection rectangle.
    /// These handles appear at the corners of a selection to allow users to resize it.
    /// </summary>
    public enum HandlePosition
    {
        TopLeft,      // Handle in the upper-left corner
        TopRight,     // Handle in the upper-right corner
        BottomLeft,   // Handle in the lower-left corner
        BottomRight   // Handle in the lower-right corner
    }

    /// <summary>
    /// A visual and interactive handle used to resize selection rectangles in the sprite atlas editor.
    /// Each selection has four handles (one at each corner) that users can drag to resize the selection.
    /// SelectionHandle inherits from Canvas to provide a container for the visual elements and handle mouse input.
    /// </summary>
    public class SelectionHandle : Canvas
    {
        /// <summary>
        /// Gets the position of this handle (which corner of the target rectangle it represents)
        /// </summary>
        public HandlePosition Position { get; private set; }
        
        /// <summary>
        /// Gets the target rectangle this handle is controlling/resizing
        /// </summary>
        public Rectangle TargetRectangle { get; private set; }

        // Track the original position and size during resize operations
        // These values are captured when a drag operation begins and used as reference points
        private Point dragStartPoint;      // Starting point of the drag operation
        private double originalLeft;       // Original X position of the target rectangle
        private double originalTop;        // Original Y position of the target rectangle
        private double originalWidth;      // Original width of the target rectangle
        private double originalHeight;     // Original height of the target rectangle

        // Reference to the parent window for status updates and coordinate conversion
        private readonly MainWindow parentWindow;

        // Size of the handle in screen space (will be scaled by zoom)
        // This determines the visual and clickable size of the handle
        private const double HandleSize = 8.0;

        // The visual representation of this handle (a small square at the corner)
        private readonly Rectangle visualRectangle;

        /// <summary>
        /// Creates a new selection handle at the specified position for the target rectangle
        /// </summary>
        /// <param name="position">The corner position where this handle should appear</param>
        /// <param name="targetRect">The rectangle that will be resized by this handle</param>
        /// <param name="parent">Reference to the main window for status updates and coordinate conversion</param>
        public SelectionHandle(HandlePosition position, Rectangle targetRect, MainWindow parent)
        {
            Position = position;
            TargetRectangle = targetRect;
            parentWindow = parent;

            // Create a visual representation of the handle
            // This is the small white square with black border that the user can grab and drag
            visualRectangle = new Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = Brushes.White,              // White fill for visibility
                Stroke = Brushes.Black,            // Black outline for contrast
                StrokeThickness = 1,
                Cursor = GetCursorForPosition(position)  // Different cursor based on corner position
            };

            // Add the visual rectangle to this Canvas
            // The handle itself is a Canvas, and the visual rectangle is a child of that Canvas
            this.Children.Add(visualRectangle);

            // Setup mouse events on the SelectionHandle itself
            // These event handlers manage the dragging behavior for resizing
            this.MouseLeftButtonDown += Handle_MouseLeftButtonDown;  // Start dragging
            this.MouseLeftButtonUp += Handle_MouseLeftButtonUp;      // End dragging
            this.MouseMove += Handle_MouseMove;                      // Update while dragging

            // Initial positioning of the handle based on the target rectangle
            UpdatePosition();
        }

        /// <summary>
        /// Updates the position of the handle based on the current position and size of the target rectangle.
        /// This is called whenever the target rectangle changes position or size.
        /// </summary>
        /// <param name="zoomLevel">Current zoom level of the canvas (default is 1.0)</param>
        public void UpdatePosition(double zoomLevel = 1.0)
        {
            // Get the current position and dimensions of the target rectangle
            double left = Canvas.GetLeft(TargetRectangle);
            double top = Canvas.GetTop(TargetRectangle);
            double width = TargetRectangle.Width;
            double height = TargetRectangle.Height;

            // Adjust handle size for zoom
            // When zoomed in, handles appear smaller relative to the content
            visualRectangle.Width = visualRectangle.Height = HandleSize / zoomLevel;
            visualRectangle.StrokeThickness = 1 / zoomLevel;

            // Position the SelectionHandle itself on the parent canvas
            // Each handle is positioned at a different corner of the target rectangle
            switch (Position)
            {
                case HandlePosition.TopLeft:
                    // Position at the upper-left corner
                    Canvas.SetLeft(this, left - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top - visualRectangle.Height / 2);
                    break;
                case HandlePosition.TopRight:
                    // Position at the upper-right corner
                    Canvas.SetLeft(this, left + width - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top - visualRectangle.Height / 2);
                    break;
                case HandlePosition.BottomLeft:
                    // Position at the lower-left corner
                    Canvas.SetLeft(this, left - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top + height - visualRectangle.Height / 2);
                    break;
                case HandlePosition.BottomRight:
                    // Position at the lower-right corner
                    Canvas.SetLeft(this, left + width - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top + height - visualRectangle.Height / 2);
                    break;
            }
            
            // The visual rectangle is positioned at (0,0) within its parent Canvas
            // This centers the visual rectangle within the SelectionHandle Canvas
            Canvas.SetLeft(visualRectangle, 0);
            Canvas.SetTop(visualRectangle, 0);
        }

        /// <summary>
        /// Returns the appropriate cursor type based on the handle position.
        /// This provides visual feedback to the user about how they can resize the selection.
        /// </summary>
        /// <param name="position">The corner position of the handle</param>
        /// <returns>The appropriate cursor for the specified position</returns>
        private static Cursor GetCursorForPosition(HandlePosition position)
        {
            // Return diagonal resize cursors based on the handle position
            // This provides a visual cue about the direction the selection will resize
            return position switch
            {
                // Northwest-Southeast resize cursor for top-left and bottom-right corners
                HandlePosition.TopLeft or HandlePosition.BottomRight => Cursors.SizeNWSE,
                
                // Northeast-Southwest resize cursor for top-right and bottom-left corners
                HandlePosition.TopRight or HandlePosition.BottomLeft => Cursors.SizeNESW,
                
                // Default arrow cursor as a fallback (should never occur)
                _ => Cursors.Arrow
            };
        }

        /// <summary>
        /// Handles the start of a drag operation when the user presses the left mouse button on a handle.
        /// Records the initial state before resizing begins.
        /// </summary>
        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the original rectangle properties before resizing
            // These values are used as reference points during the drag operation
            originalLeft = Canvas.GetLeft(TargetRectangle);
            originalTop = Canvas.GetTop(TargetRectangle);
            originalWidth = TargetRectangle.Width;
            originalHeight = TargetRectangle.Height;

            // Store the mouse start position and convert to unzoomed coordinates
            // This ensures we're tracking in the same coordinate space as the rectangle
            // SnapToPixel converts mouse coordinates accounting for zoom level
            dragStartPoint = parentWindow.SnapToPixel(e.GetPosition((Canvas)this.Parent));

            // Capture the mouse to receive mouse events even if the cursor moves outside the handle
            // This allows for smooth resizing even with rapid mouse movements
            this.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the mouse movement during a drag operation to resize the target rectangle.
        /// This method calculates new dimensions based on the handle being dragged and updates the rectangle.
        /// </summary>
        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            // Only process mouse movement if we're in the middle of a drag operation
            if (this.IsMouseCaptured)
            {
                // Get current mouse position and convert to unzoomed coordinates
                // This ensures consistent behavior regardless of zoom level
                Point currentPoint = parentWindow.SnapToPixel(e.GetPosition((Canvas)this.Parent));
                
                // Calculate how far the mouse has moved from the starting point
                double deltaX = currentPoint.X - dragStartPoint.X;
                double deltaY = currentPoint.Y - dragStartPoint.Y;

                // Initialize new rectangle properties with the original values
                // These will be modified based on which handle is being dragged
                double newLeft = originalLeft;
                double newTop = originalTop;
                double newWidth = originalWidth;
                double newHeight = originalHeight;

                // Calculate new rectangle dimensions based on which handle is being dragged
                // Each handle affects different sides of the rectangle
                switch (Position)
                {
                    case HandlePosition.TopLeft:
                        // Dragging top-left affects left, top, width, and height
                        // Moving this handle inward makes the rectangle smaller
                        newLeft = originalLeft + deltaX;
                        newTop = originalTop + deltaY;
                        newWidth = originalWidth - deltaX;
                        newHeight = originalHeight - deltaY;
                        break;
                    case HandlePosition.TopRight:
                        // Dragging top-right affects top, width, and height
                        // Moving right increases width, moving up decreases height
                        newTop = originalTop + deltaY;
                        newWidth = originalWidth + deltaX;
                        newHeight = originalHeight - deltaY;
                        break;
                    case HandlePosition.BottomLeft:
                        // Dragging bottom-left affects left, width, and height
                        // Moving left decreases width, moving down increases height
                        newLeft = originalLeft + deltaX;
                        newWidth = originalWidth - deltaX;
                        newHeight = originalHeight + deltaY;
                        break;
                    case HandlePosition.BottomRight:
                        // Dragging bottom-right affects width and height
                        // Moving this handle outward makes the rectangle larger
                        newWidth = originalWidth + deltaX;
                        newHeight = originalHeight + deltaY;
                        break;
                }

                // Ensure minimum width and height (1 pixel)
                // Prevents the rectangle from disappearing or becoming invalid
                if (newWidth < 1)
                {
                    newWidth = 1;
                    // Adjust left position if we're dragging left side handles
                    if (Position == HandlePosition.TopLeft || Position == HandlePosition.BottomLeft)
                        newLeft = originalLeft + originalWidth - 1;
                }

                if (newHeight < 1)
                {
                    newHeight = 1;
                    // Adjust top position if we're dragging top handles
                    if (Position == HandlePosition.TopLeft || Position == HandlePosition.TopRight)
                        newTop = originalTop + originalHeight - 1;
                }

                // Round to integer values for pixel-perfect alignment
                // This ensures selections align perfectly with pixels in the sprite sheet
                newLeft = Math.Floor(newLeft);
                newTop = Math.Floor(newTop);
                newWidth = Math.Floor(newWidth);
                newHeight = Math.Floor(newHeight);

                // Apply the new dimensions to the target rectangle
                Canvas.SetLeft(TargetRectangle, newLeft);
                Canvas.SetTop(TargetRectangle, newTop);
                TargetRectangle.Width = newWidth;
                TargetRectangle.Height = newHeight;

                // Update the positions of all handles on the selection
                // This ensures all four corner handles stay properly positioned
                parentWindow.UpdateHandlePositions();

                // Update status display to show current rectangle dimensions
                // This shows the user the exact position and size in the UI
                parentWindow.UpdateSelectionStatus(newLeft, newTop, newWidth, newHeight);
            }
        }

        /// <summary>
        /// Handles the end of a drag operation when the user releases the left mouse button.
        /// Releases mouse capture so other UI elements can receive mouse events.
        /// </summary>
        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Check if we're currently capturing mouse events
            if (this.IsMouseCaptured)
            {
                // Release the mouse capture to end the drag operation
                this.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
    }
}