using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// Manages a collection of sprite items in the ATLAS Plotter application.
    /// This class serves as the central data model for all sprites in the sprite atlas.
    /// It handles creation, selection, and removal of sprites, as well as assigning unique 
    /// colors to each sprite for visual identification on the canvas.
    /// </summary>
    /// <remarks>
    /// This class implements INotifyPropertyChanged to enable data binding with the UI.
    /// When properties change, the UI is automatically notified to update.
    /// </remarks>
    public class SpriteItemCollection : INotifyPropertyChanged
    {
        /// <summary>
        /// The collection of sprite items managed by this collection.
        /// Using ObservableCollection enables automatic UI updates when items are added or removed.
        /// </summary>
        private ObservableCollection<SpriteItem> _items = [];
        public ObservableCollection<SpriteItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The currently selected sprite item in the editor.
        /// When this changes, the OnSelectedItemChanged event is fired to notify listeners.
        /// </summary>
        /// <remarks>
        /// This property is bound to the UI selection and controls which sprite is being edited.
        /// When a user clicks on a sprite in the list or on the canvas, this property is updated.
        /// </remarks>
        private SpriteItem _selectedItem;
        public SpriteItem SelectedItem
        {
            get => _selectedItem;
            set 
            { 
                _selectedItem = value; 
                OnPropertyChanged();
                // Notify listeners (like MainWindow and SpriteItemMarkers) that selection has changed
                OnSelectedItemChanged?.Invoke(this, _selectedItem);
            }
        }

        /// <summary>
        /// Dictionary mapping sprite IDs to their assigned display colors.
        /// This ensures each sprite has a consistent color in the editor for visual identification.
        /// </summary>
        private readonly Dictionary<int, Color> _itemColors = [];
        
        /// <summary>
        /// Random number generator used for assigning colors to sprites.
        /// </summary>
        private readonly Random _random = new Random();

        /// <summary>
        /// Event that fires when the selected sprite item changes.
        /// MainWindow and SpriteItemMarker subscribe to this event to update the UI.
        /// </summary>
        public event EventHandler<SpriteItem> OnSelectedItemChanged;
        
        /// <summary>
        /// Event required for INotifyPropertyChanged implementation.
        /// This event notifies the UI when properties change so it can update bindings.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Constructs a new sprite collection and adds one default item.
        /// </summary>
        public SpriteItemCollection()
        {
            // Create a default sprite item so the collection isn't empty on startup
            AddNewItem();
        }

        /// <summary>
        /// Adds a new sprite item to the collection without executing a command.
        /// Used internally by commands to implement undo/redo.
        /// </summary>
        /// <returns>The newly created sprite item</returns>
        internal SpriteItem AddNewItemInternal()
        {
            // Create a new sprite with default values
            var newItem = CreateDefaultSpriteItem();
            
            // Assign a color for this sprite
            AssignRandomColor(newItem.Id);
            
            // Add to the collection and select it
            Items.Add(newItem);
            SelectedItem = newItem;
            
            return newItem;
        }

        /// <summary>
        /// Adds an existing sprite item to the collection without executing a command.
        /// Used internally by commands to implement undo/redo.
        /// </summary>
        /// <param name="item">The sprite item to add</param>
        /// <param name="index">Optional index to insert at (default is at the end)</param>
        internal void AddExistingItemInternal(SpriteItem item, int index = -1)
        {
            // Assign a color for this sprite if it doesn't already have one
            if (!_itemColors.ContainsKey(item.Id))
            {
                AssignRandomColor(item.Id);
            }
            
            // Add at specific index or at the end
            if (index >= 0 && index <= Items.Count)
            {
                Items.Insert(index, item);
            }
            else
            {
                Items.Add(item);
            }
            
            // Select the added item
            SelectedItem = item;
        }

        /// <summary>
        /// Removes a sprite item from the collection without executing a command.
        /// Used internally by commands to implement undo/redo.
        /// </summary>
        /// <param name="item">The sprite item to remove</param>
        internal void RemoveItemInternal(SpriteItem item)
        {
            if (Items.Contains(item))
            {
                // Find if this was the selected item
                bool wasSelected = (SelectedItem == item);
                
                // Remove from the collection
                Items.Remove(item);
                
                // Update selection if needed
                if (wasSelected)
                {
                    SelectedItem = Items.Count > 0 ? Items[0] : null!;
                }
            }
        }

        /// <summary>
        /// Adds a new sprite item to the collection with default properties.
        /// The new sprite is automatically selected after being added.
        /// </summary>
        /// <remarks>
        /// This method creates and executes an AddSpriteCommand for undo/redo tracking.
        /// </remarks>
        public void AddNewItem()
        {
            // Create a command for adding a sprite
            var command = new Commands.AddSpriteCommand(this, null);
            
            // Execute the command through the command manager
            Commands.CommandManager.Instance.ExecuteCommand(command);
        }

        /// <summary>
        /// Removes a sprite item from the collection.
        /// If the removed item was selected, another item will be selected if available.
        /// </summary>
        /// <param name="item">The sprite item to remove</param>
        /// <remarks>
        /// This method creates and executes a RemoveSpriteCommand for undo/redo tracking.
        /// </remarks>
        public void RemoveItem(SpriteItem item)
        {
            if (Items.Contains(item))
            {
                // Create a command for removing a sprite
                var command = new Commands.RemoveSpriteCommand(this, item, null, null);
                
                // Execute the command through the command manager
                Commands.CommandManager.Instance.ExecuteCommand(command);
            }
        }

        /// <summary>
        /// Gets the color assigned to a specific sprite by its ID.
        /// This color is used for visualization in SpriteItemMarker.
        /// </summary>
        /// <param name="id">The ID of the sprite</param>
        /// <returns>The color assigned to the sprite, or red if not found</returns>
        /// <remarks>
        /// SpriteItemMarker calls this method to determine what color to use for 
        /// highlighting a sprite's boundaries on the canvas.
        /// </remarks>
        public Color GetItemColor(int id)
        {
            // Using TryGetValue instead of ContainsKey + indexer to avoid double dictionary lookup
            if (_itemColors.TryGetValue(id, out Color color))
                return color;
            
            return Colors.Red; // Default color if ID not found
        }

        /// <summary>
        /// Assigns a random bright color to a sprite for visual identification.
        /// </summary>
        /// <param name="id">The ID of the sprite to assign a color to</param>
        /// <remarks>
        /// This ensures each sprite has a distinct color in the editor,
        /// making it easier to identify different sprites on the canvas.
        /// </remarks>
        private void AssignRandomColor(int id)
        {
            // Generate a random bright color (values between 100-255 for RGB)
            // Lower values are avoided to ensure colors are bright enough to see
            Color newColor = Color.FromRgb(
                (byte)_random.Next(100, 255),
                (byte)_random.Next(100, 255),
                (byte)_random.Next(100, 255));
            
            // Store the color mapped to this sprite's ID
            _itemColors[id] = newColor;
        }

        /// <summary>
        /// Creates a new sprite item with default values for all properties.
        /// These values can be edited by the user later through the UI.
        /// </summary>
        /// <returns>A new sprite item with default values</returns>
        /// <remarks>
        /// This method sets reasonable default values for all sprite properties
        /// so the user has a starting point to work with.
        /// </remarks>
        private SpriteItem CreateDefaultSpriteItem()
        {
            // Create a sprite with predefined default values
            // These values represent a typical sprite configuration that 
            // the user can modify through the JsonDataEntryControl
            var sprite = new SpriteItem
            {
                Id = GenerateUniqueId(),                              // Unique identifier
                Name = $"sprite_{DateTime.Now.Ticks % 10000}",        // Default name with unique number
                YSort = 10,                                          // Default Y-sorting value
                Fragile = true,                                      // Can be broken
                Breakable = true,                                    // Can be broken
                
                // Position offset for rendering the sprite
                Offset = new PointOffset { X = 3, Y = 3 },
                
                // Rectangle defining where in the atlas the sprite is located
                // This is what the PixelLocationDisplay and SpriteItemMarker help set
                Source = new RectSource { X = 0, Y = 0, Width = 10, Height = 9 },
                
                // Shadow offset and source (if sprite has a shadow)
                ShadowOffset = new PointOffset { X = 4, Y = 8 },
                ShadowSource = new RectSource { X = 1, Y = 18, Width = 8, Height = 6 },
                
                // Default collision area for the sprite
                Colliders =
                [
                    new Collider { Type = "rectangle", X = 4, Y = 4, Width = 8, Height = 8 }
                ],
                
                // Animation data for when the sprite breaks
                BreakingAnimation = new BreakingAnimation
                {
                    Source = new RectSource { X = 0, Y = 284, Width = 28, Height = 20 },
                    Offset = new PointOffset { X = -3, Y = -6 },
                    XInverted = -9,
                    FrameDuration = 75,
                    NbFrames = 4
                }
            };
            
            return sprite;
        }

        /// <summary>
        /// Generates a unique ID for a new sprite item.
        /// </summary>
        /// <returns>A unique ID that doesn't conflict with existing sprites</returns>
        private int GenerateUniqueId()
        {
            // Start with a base ID of 1000
            int maxId = 1000;
            
            // Find the highest existing ID and add 1 to ensure uniqueness
            foreach (var item in Items)
            {
                if (item.Id > maxId)
                    maxId = item.Id;
            }
            return maxId + 1;
        }

        /// <summary>
        /// Invokes the PropertyChanged event for data binding.
        /// </summary>
        /// <param name="name">Name of the property that changed (automatically determined if not specified)</param>
        /// <remarks>
        /// This method is called whenever a property changes to notify the UI.
        /// The CallerMemberName attribute automatically provides the calling property's name.
        /// </remarks>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}