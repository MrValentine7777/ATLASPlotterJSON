using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ATLASPlotterJSON
{
    public enum HandlePosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    // Changed SelectionHandle to inherit from Canvas (which is a UIElement)
    public class SelectionHandle : Canvas
    {
        public HandlePosition Position { get; private set; }
        public Rectangle TargetRectangle { get; private set; }

        // Track the original position and size during resize operations
        private Point dragStartPoint;
        private double originalLeft;
        private double originalTop;
        private double originalWidth;
        private double originalHeight;

        // Reference to the parent window for status updates
        private MainWindow parentWindow;

        // Size of the handle in screen space (will be scaled by zoom)
        private const double HandleSize = 8.0;

        private Rectangle visualRectangle;

        public SelectionHandle(HandlePosition position, Rectangle targetRect, MainWindow parent)
        {
            Position = position;
            TargetRectangle = targetRect;
            parentWindow = parent;

            // Create a visual representation of the handle
            visualRectangle = new Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Cursor = GetCursorForPosition(position)
            };

            // Add the visual rectangle to this Canvas
            this.Children.Add(visualRectangle);

            // Setup mouse events on the SelectionHandle itself
            this.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            this.MouseLeftButtonUp += Handle_MouseLeftButtonUp;
            this.MouseMove += Handle_MouseMove;

            // Initial positioning
            UpdatePosition();
        }

        public void UpdatePosition(double zoomLevel = 1.0)
        {
            double left = Canvas.GetLeft(TargetRectangle);
            double top = Canvas.GetTop(TargetRectangle);
            double width = TargetRectangle.Width;
            double height = TargetRectangle.Height;

            // Adjust handle size for zoom
            visualRectangle.Width = visualRectangle.Height = HandleSize / zoomLevel;
            visualRectangle.StrokeThickness = 1 / zoomLevel;

            // Position the SelectionHandle itself on the parent canvas
            switch (Position)
            {
                case HandlePosition.TopLeft:
                    Canvas.SetLeft(this, left - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top - visualRectangle.Height / 2);
                    break;
                case HandlePosition.TopRight:
                    Canvas.SetLeft(this, left + width - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top - visualRectangle.Height / 2);
                    break;
                case HandlePosition.BottomLeft:
                    Canvas.SetLeft(this, left - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top + height - visualRectangle.Height / 2);
                    break;
                case HandlePosition.BottomRight:
                    Canvas.SetLeft(this, left + width - visualRectangle.Width / 2);
                    Canvas.SetTop(this, top + height - visualRectangle.Height / 2);
                    break;
            }
            
            // The visual rectangle is positioned at (0,0) within its parent Canvas
            Canvas.SetLeft(visualRectangle, 0);
            Canvas.SetTop(visualRectangle, 0);
        }

        private Cursor GetCursorForPosition(HandlePosition position)
        {
            return position switch
            {
                HandlePosition.TopLeft or HandlePosition.BottomRight => Cursors.SizeNWSE,
                HandlePosition.TopRight or HandlePosition.BottomLeft => Cursors.SizeNESW,
                _ => Cursors.Arrow
            };
        }

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the original rectangle properties before resizing
            originalLeft = Canvas.GetLeft(TargetRectangle);
            originalTop = Canvas.GetTop(TargetRectangle);
            originalWidth = TargetRectangle.Width;
            originalHeight = TargetRectangle.Height;

            // Store the mouse start position
            dragStartPoint = e.GetPosition((Canvas)this.Parent);

            // Capture the mouse
            this.CaptureMouse();
            e.Handled = true;
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.IsMouseCaptured)
            {
                // Get current mouse position and calculate delta
                Point currentPoint = parentWindow.SnapToPixel(e.GetPosition((Canvas)this.Parent));
                double deltaX = currentPoint.X - dragStartPoint.X / parentWindow.CurrentZoom;
                double deltaY = currentPoint.Y - dragStartPoint.Y / parentWindow.CurrentZoom;

                // Calculate new rectangle dimensions based on which handle is being dragged
                double newLeft = originalLeft;
                double newTop = originalTop;
                double newWidth = originalWidth;
                double newHeight = originalHeight;

                switch (Position)
                {
                    case HandlePosition.TopLeft:
                        newLeft = originalLeft + deltaX;
                        newTop = originalTop + deltaY;
                        newWidth = originalWidth - deltaX;
                        newHeight = originalHeight - deltaY;
                        break;
                    case HandlePosition.TopRight:
                        newTop = originalTop + deltaY;
                        newWidth = originalWidth + deltaX;
                        newHeight = originalHeight - deltaY;
                        break;
                    case HandlePosition.BottomLeft:
                        newLeft = originalLeft + deltaX;
                        newWidth = originalWidth - deltaX;
                        newHeight = originalHeight + deltaY;
                        break;
                    case HandlePosition.BottomRight:
                        newWidth = originalWidth + deltaX;
                        newHeight = originalHeight + deltaY;
                        break;
                }

                // Ensure minimum width and height (1 pixel)
                if (newWidth < 1)
                {
                    newWidth = 1;
                    if (Position == HandlePosition.TopLeft || Position == HandlePosition.BottomLeft)
                        newLeft = originalLeft + originalWidth - 1;
                }

                if (newHeight < 1)
                {
                    newHeight = 1;
                    if (Position == HandlePosition.TopLeft || Position == HandlePosition.TopRight)
                        newTop = originalTop + originalHeight - 1;
                }

                // Round to integer values for pixel-perfect alignment
                newLeft = Math.Floor(newLeft);
                newTop = Math.Floor(newTop);
                newWidth = Math.Floor(newWidth);
                newHeight = Math.Floor(newHeight);

                // Apply the new dimensions to the target rectangle
                Canvas.SetLeft(TargetRectangle, newLeft);
                Canvas.SetTop(TargetRectangle, newTop);
                TargetRectangle.Width = newWidth;
                TargetRectangle.Height = newHeight;

                // Update the positions of all handles
                parentWindow.UpdateHandlePositions();

                // Update status display
                parentWindow.UpdateSelectionStatus(newLeft, newTop, newWidth, newHeight);
            }
        }

        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
    }
}