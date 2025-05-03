using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// Interaction logic for JsonDataEntryControl.xaml
    /// 
    /// COMPONENT OVERVIEW:
    /// This is the form/panel that appears on the right side of the application.
    /// It allows users to view and edit properties of sprite items, including:
    ///   - Basic properties (ID, name, etc.)
    ///   - Position and size within the atlas image
    ///   - Colliders (hitboxes)
    ///   - Animation data
    /// 
    /// ARCHITECTURAL ROLE:
    /// This component serves as the primary data entry interface for the application.
    /// It connects the visual canvas (where sprites are displayed and positioned) with 
    /// the underlying data models (SpriteItems and their properties).
    /// 
    /// KEY RELATIONSHIPS:
    /// - Contains a SpriteItemCollection that holds all sprite data
    /// - Communicates with MainWindow through events (SpriteAdded, SpriteRemoved)
    /// - Provides UI for editing data in JsonDataModels.cs classes
    /// </summary>
    public partial class JsonDataEntryControl : UserControl
    {
        /// <summary>
        /// The collection that holds all sprite items in the project.
        /// This is the central data store that maintains the list of sprites and their properties.
        /// The SpriteItemCollection handles sprite creation, selection, color assignment, etc.
        /// </summary>
        private SpriteItemCollection _spriteCollection = new SpriteItemCollection();
        
        /// <summary>
        /// COMMUNICATION SYSTEM - EVENTS:
        /// 
        /// Event that notifies the MainWindow when a different sprite is selected.
        /// This allows the MainWindow to update the visual marker highlighting on the canvas.
        /// 
        /// EVENT FLOW:
        /// 1. User selects a sprite in this control's dropdown
        /// 2. This event fires to notify MainWindow
        /// 3. MainWindow updates all SpriteItemMarkers to highlight the selected one
        /// </summary>
        public event EventHandler<SpriteItem>? SelectedSpriteChanged;
        
        /// <summary>
        /// COMMUNICATION SYSTEM - EVENTS:
        /// 
        /// Events that notify the MainWindow when sprites are added or removed.
        /// This allows MainWindow to create or remove the corresponding visual markers.
        /// 
        /// EVENT FLOW:
        /// 1. User adds/removes a sprite in this control
        /// 2. These events fire to notify MainWindow
        /// 3. MainWindow creates/removes SpriteItemMarkers on the canvas
        /// </summary>
        public event EventHandler<SpriteItem>? SpriteAdded;
        public event EventHandler<SpriteItem>? SpriteRemoved;

        /// <summary>
        /// Public access to the sprite collection for other components.
        /// This allows MainWindow to access the collection of sprites.
        /// </summary>
        public SpriteItemCollection SpriteCollection => _spriteCollection;

        /// <summary>
        /// Creates a new JsonDataEntryControl and initializes the UI.
        /// </summary>
        public JsonDataEntryControl()
        {
            // Initialize the WPF controls defined in XAML
            InitializeComponent();
            
            // DATA BINDING CONNECTION:
            // Link the UI to our sprite collection using WPF's data binding system
            // This enables two-way updates: changes in UI update the data, and vice versa
            this.DataContext = _spriteCollection;
            
            // EVENT CONNECTION:
            // Subscribe to sprite selection changes from the collection
            // When a sprite is selected in the dropdown, we need to notify MainWindow
            _spriteCollection.OnSelectedItemChanged += (s, item) => 
            {
                // Forward the event to anyone listening (MainWindow)
                // This is called "event bubbling" - passing events up the chain
                SelectedSpriteChanged?.Invoke(this, item);
            };
        }

        /// <summary>
        /// Updates the position of the selected sprite based on a canvas click location.
        /// This method is called by MainWindow when the user clicks on the canvas.
        /// 
        /// COMPONENT CONNECTION:
        /// MainWindow → JsonDataEntryControl → SpriteItem
        /// (Canvas click) → (This method) → (Update sprite position)
        /// </summary>
        /// <param name="location">The pixel coordinates where the user clicked</param>
        public void SetCurrentLocation(Point location)
        {
            // Make sure we have a selected sprite to update
            if (_spriteCollection?.SelectedItem != null)
            {
                // DATA FLOW:
                // Update the sprite's position in the atlas to match where the user clicked
                // This will automatically update the UI due to data binding
                _spriteCollection.SelectedItem.Source.X = (int)location.X;
                _spriteCollection.SelectedItem.Source.Y = (int)location.Y;
                
                // NOTE: After this change, several things happen automatically:
                // 1. The sprite's properties in the form update (due to data binding)
                // 2. The SpriteItemMarker on the canvas moves (due to property change events)
            }
        }

        /// <summary>
        /// Handles the "Add Collider" button click.
        /// Adds a default rectangular collider to the selected sprite.
        /// 
        /// GAME DEVELOPMENT CONCEPT:
        /// Colliders define the parts of a sprite that can interact with other game elements.
        /// In many games, the collision area is smaller than the visual sprite.
        /// </summary>
        private void AddCollider_Click(object sender, RoutedEventArgs e)
        {
            // Make sure we have a selected sprite
            if (_spriteCollection.SelectedItem != null)
            {
                // USER INTERACTION:
                // Add a new collider with default values to the sprite
                // The user can then adjust its size and position as needed
                _spriteCollection.SelectedItem.Colliders.Add(new Collider
                {
                    Type = "rectangle",    // Currently only rectangles are supported
                    X = 0,                 // Position relative to sprite's top-left
                    Y = 0,
                    Width = 8,             // Default small size
                    Height = 8
                });
                
                // DATA BINDING NOTE:
                // The UI list of colliders updates automatically 
                // because Colliders is an observable collection
            }
        }

        /// <summary>
        /// Handles the "Remove Collider" button click.
        /// Removes the specified collider from the selected sprite.
        /// 
        /// WPF TECHNIQUE:
        /// This method uses the button's Tag property to identify which collider to remove.
        /// Each remove button in the XAML has its collider bound to its Tag.
        /// </summary>
        private void RemoveCollider_Click(object sender, RoutedEventArgs e)
        {
            // DATA FLOW:
            // 1. Check if the sender is a button with a collider Tag
            // 2. If so, and we have a selected sprite, remove that collider
            if (sender is Button button && button.Tag is Collider collider && 
                _spriteCollection.SelectedItem != null)
            {
                // Remove the collider from the sprite
                _spriteCollection.SelectedItem.Colliders.Remove(collider);
                
                // DATA BINDING NOTE:
                // The UI updates automatically to remove the collider's form fields
                // because Colliders is an observable collection
            }
        }
        
        /// <summary>
        /// Handles the "Add Sprite" button click.
        /// Creates a new sprite item with default values and adds it to the collection.
        /// 
        /// COMPONENT CONNECTION:
        /// This method connects to:
        /// - SpriteItemCollection.AddNewItem (to create the sprite)
        /// - MainWindow (via the SpriteAdded event)
        /// </summary>
        private void AddSprite_Click(object sender, RoutedEventArgs e)
        {
            // ARCHITECTURAL FLOW:
            // 1. Call the collection to create and add a new sprite with default values
            _spriteCollection.AddNewItem();
            
            // 2. Notify listeners (MainWindow) that a sprite was added
            // This allows MainWindow to create a visual marker for this sprite
            SpriteAdded?.Invoke(this, _spriteCollection.SelectedItem);
            
            // DATA BINDING NOTE:
            // The dropdown list updates automatically to show the new sprite
            // because Items is an observable collection
        }
        
        /// <summary>
        /// Handles the "Remove Sprite" button click.
        /// Removes the currently selected sprite from the collection.
        /// 
        /// COMPONENT CONNECTION:
        /// This method connects to:
        /// - SpriteItemCollection.RemoveItem (to remove the sprite)
        /// - MainWindow (via the SpriteRemoved event)
        /// </summary>
        private void RemoveSprite_Click(object sender, RoutedEventArgs e)
        {
            // Make sure we have a selected sprite
            if (_spriteCollection.SelectedItem != null)
            {
                // Store the item to remove so we can reference it after removal
                var itemToRemove = _spriteCollection.SelectedItem;
                
                // ARCHITECTURAL FLOW:
                // 1. Remove the sprite from the collection
                _spriteCollection.RemoveItem(itemToRemove);
                
                // 2. Notify listeners (MainWindow) that a sprite was removed
                // This allows MainWindow to remove the visual marker for this sprite
                SpriteRemoved?.Invoke(this, itemToRemove);
                
                // DATA BINDING NOTE:
                // The dropdown list updates automatically to remove the sprite
                // because Items is an observable collection
            }
        }

        /// <summary>
        /// Handles the "Load JSON" button click.
        /// Displays a file picker and loads sprite data from a JSON file.
        /// 
        /// DATA FLOW:
        /// JSON file → Deserialized into objects → Updated in UI
        /// 
        /// This method can load either:
        /// - A full SpriteItemCollection with multiple sprites
        /// - A single SpriteItem (for importing individual sprites)
        /// </summary>
        private void LoadJson_Click(object sender, RoutedEventArgs e)
        {
            // Create and configure a file picker dialog
            var openFileDialog = new OpenFileDialog
            {
                Title = "Load JSON Data",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            // Show the dialog and process the result if user selected a file
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Read the entire JSON file as text
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    
                    // Configure JSON deserialization options
                    // PropertyNameCaseInsensitive allows for flexibility in JSON property names
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    // DECISION POINT:
                    // Determine whether this is a collection or a single sprite
                    // by checking for the "Items" property in the JSON
                    if (jsonContent.Contains("\"Items\":"))
                    {
                        // SCENARIO 1: Loading a full collection
                        // DATA FLOW: 
                        // JSON text → SpriteItemCollection object
                        SpriteItemCollection? loadedCollection = JsonSerializer.Deserialize<SpriteItemCollection>(jsonContent, options);
                        
                        if (loadedCollection != null)
                        {
                            // Replace the current collection with the loaded one
                            _spriteCollection = loadedCollection;
                            
                            // Update the UI binding to the new collection
                            this.DataContext = _spriteCollection;
                            
                            // COMPONENT CONNECTION:
                            // Notify MainWindow about each loaded sprite
                            // This allows MainWindow to create visual markers for all sprites
                            foreach (var sprite in _spriteCollection.Items)
                            {
                                SpriteAdded?.Invoke(this, sprite);
                            }
                            
                            // Show success message with sprite count
                            MessageBox.Show($"Loaded {_spriteCollection.Items.Count} sprite items.", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        // SCENARIO 2: Loading a single sprite
                        // DATA FLOW:
                        // JSON text → SpriteItem object
                        SpriteItem? loadedItem = JsonSerializer.Deserialize<SpriteItem>(jsonContent, options);
                        
                        if (loadedItem != null)
                        {
                            // Create a new collection with just this sprite
                            _spriteCollection = new SpriteItemCollection();
                            _spriteCollection.Items.Clear();  // Clear default item
                            _spriteCollection.Items.Add(loadedItem);
                            _spriteCollection.SelectedItem = loadedItem;
                            
                            // Update the UI binding to the new collection
                            this.DataContext = _spriteCollection;
                            
                            // COMPONENT CONNECTION:
                            // Notify MainWindow about the loaded sprite
                            // This allows MainWindow to create a visual marker
                            SpriteAdded?.Invoke(this, loadedItem);
                            
                            // Show success message
                            MessageBox.Show("Single sprite item loaded successfully!", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ERROR HANDLING:
                    // Show details about what went wrong during loading
                    MessageBox.Show($"Error loading JSON: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handles the "Save JSON" button click.
        /// Displays a save dialog and writes sprite data to a JSON file.
        /// 
        /// DATA FLOW:
        /// SpriteItemCollection → Serialized to JSON → Saved to file
        /// 
        /// The resulting JSON file can be used in other applications,
        /// like game engines that need to know sprite locations and properties.
        /// </summary>
        private void SaveJson_Click(object sender, RoutedEventArgs e)
        {
            // Create and configure a save file dialog
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save JSON Data",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json"
            };

            // Show the dialog and process the result if user chose a location
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Configure JSON serialization options
                    // WriteIndented creates nicely formatted JSON with line breaks and indentation
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    // DATA FLOW:
                    // SpriteItemCollection → JSON text
                    // This includes ALL sprites and their properties
                    string jsonContent = JsonSerializer.Serialize(_spriteCollection, options);
                    
                    // Write the JSON to the selected file
                    File.WriteAllText(saveFileDialog.FileName, jsonContent);
                    
                    // Show success message with sprite count
                    MessageBox.Show($"JSON data saved successfully with {_spriteCollection.Items.Count} sprites!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // ERROR HANDLING:
                    // Show details about what went wrong during saving
                    MessageBox.Show($"Error saving JSON: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}