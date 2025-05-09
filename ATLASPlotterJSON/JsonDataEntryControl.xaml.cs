﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ATLASPlotterJSON.Commands;

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
        /// Cached JSON serializer options for loading JSON (deserialization).
        /// Configured to be case-insensitive for more flexible JSON parsing.
        /// </summary>
        private static readonly JsonSerializerOptions _deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Cached JSON serializer options for saving JSON (serialization).
        /// Configured to create nicely formatted, human-readable JSON files.
        /// Static and public so it can be shared with MainWindow.
        /// </summary>
        public static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

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
        /// COMMUNICATION SYSTEM - EVENTS:
        /// 
        /// Event that notifies the MainWindow when a sprite property changes.
        /// This allows the MainWindow to update the visual marker on the canvas.
        /// 
        /// EVENT FLOW:
        /// 1. User edits a sprite property in this control's forms
        /// 2. This event fires to notify MainWindow
        /// 3. MainWindow updates the corresponding SpriteItemMarker
        /// </summary>
        public event EventHandler<SpriteItem>? SpritePropertyChanged;

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
            
            // Initialize the command system for undo/redo
            InitializeCommandSystem();
            
            // EVENT CONNECTION:
            // Subscribe to sprite selection changes from the collection
            // When a sprite is selected in the dropdown, we need to notify MainWindow
            _spriteCollection.OnSelectedItemChanged += (s, item) => 
            {
                // Forward the event to anyone listening (MainWindow)
                // This is called "event bubbling" - passing events up the chain
                SelectedSpriteChanged?.Invoke(this, item);
                
                // When a sprite is selected, subscribe to its Source property changes
                if (item != null)
                {
                    SubscribeToSourcePropertyChanges(item);
                }
            };
        }

        /// <summary>
        /// Initializes the command management system.
        /// </summary>
        private void InitializeCommandSystem()
        {
            // Connect to CommandManager's state change events for UI updates
            CommandManager.Instance.CommandStateChanged += (s, cmd) =>
            {
                // Update UI state for undo/redo buttons
                btnUndo.IsEnabled = CommandManager.Instance.CanUndo;
                btnRedo.IsEnabled = CommandManager.Instance.CanRedo;
                
                // Update tooltips
                btnUndo.ToolTip = CommandManager.Instance.UndoCommandName;
                btnRedo.ToolTip = CommandManager.Instance.RedoCommandName;
            };
            
            // Initialize button states
            btnUndo.IsEnabled = CommandManager.Instance.CanUndo;
            btnRedo.IsEnabled = CommandManager.Instance.CanRedo;
        }

        /// <summary>
        /// Handles the "Undo" button click.
        /// Undoes the most recent command.
        /// </summary>
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            CommandManager.Instance.Undo();
        }

        /// <summary>
        /// Handles the "Redo" button click.
        /// Redoes the most recently undone command.
        /// </summary>
        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            CommandManager.Instance.Redo();
        }

        /// <summary>
        /// Subscribes to property changes on the sprite's Source object to ensure
        /// display updates when X, Y, Width, or Height are edited directly in the UI
        /// </summary>
        /// <param name="sprite">The sprite to monitor for Source property changes</param>
        private void SubscribeToSourcePropertyChanges(SpriteItem sprite)
        {
            // Remove previous handlers if any (to avoid duplicate subscriptions)
            sprite.Source.PropertyChanged -= Source_PropertyChanged;
            
            // Monitor changes to the Source object's properties
            sprite.Source.PropertyChanged += Source_PropertyChanged;
        }

        /// <summary>
        /// Handles property changes in the Source object (X, Y, Width, Height)
        /// Updates the visual display on the canvas when these values change through direct UI editing
        /// </summary>
        /// <param name="sender">The Source object that changed</param>
        /// <param name="e">Information about which property changed</param>
        private void Source_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is RectSource source && _spriteCollection.SelectedItem != null)
            {
                // When certain properties change, create a command to track the change
                if (e.PropertyName == "X" || e.PropertyName == "Y" || 
                    e.PropertyName == "Width" || e.PropertyName == "Height")
                {
                    // Note: This is a simplified version. In a complete implementation,
                    // you'd need to store the old value before the change.
                    // For now, just notify about the change.
                    SpritePropertyChanged?.Invoke(this, _spriteCollection.SelectedItem);
                }
            }
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
                // Create a new collider with default values
                var collider = new Collider
                {
                    Type = "rectangle",
                    X = 0,
                    Y = 0,
                    Width = 8,
                    Height = 8
                };
                
                // Create a command to add the collider
                var command = new AddColliderCommand(_spriteCollection.SelectedItem, collider);
                
                // Execute the command
                CommandManager.Instance.ExecuteCommand(command);
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
            // Check that we have a valid collider to remove
            if (sender is Button button && button.Tag is Collider collider && 
                _spriteCollection.SelectedItem != null)
            {
                // Create a command to remove the collider
                var command = new RemoveColliderCommand(_spriteCollection.SelectedItem, collider);
                
                // Execute the command
                CommandManager.Instance.ExecuteCommand(command);
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
            // Create a command for adding a sprite
            var command = new AddSpriteCommand(_spriteCollection, (item) => 
            {
                // This callback will be called when the sprite is added
                // (including during redo operations)
                SpriteAdded?.Invoke(this, item);
            });
            
            // Execute the command
            CommandManager.Instance.ExecuteCommand(command);
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
                var itemToRemove = _spriteCollection.SelectedItem;
                
                // Create a command for removing the sprite
                var command = new RemoveSpriteCommand(
                    _spriteCollection, 
                    itemToRemove,
                    (item) => SpriteRemoved?.Invoke(this, item),
                    (item) => SpriteAdded?.Invoke(this, item)
                );
                
                // Execute the command
                CommandManager.Instance.ExecuteCommand(command);
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
                    
                    // DECISION POINT:
                    // Determine whether this is a collection or a single sprite
                    // by checking for the "Items" property in the JSON
                    if (jsonContent.Contains("\"Items\":"))
                    {
                        // SCENARIO 1: Loading a full collection
                        // DATA FLOW: 
                        // JSON text → SpriteItemCollection object
                        // Using cached deserialize options instead of creating new ones
                        SpriteItemCollection? loadedCollection = JsonSerializer.Deserialize<SpriteItemCollection>(jsonContent, _deserializeOptions);
                        
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
                        // Using cached deserialize options instead of creating new ones
                        SpriteItem? loadedItem = JsonSerializer.Deserialize<SpriteItem>(jsonContent, _deserializeOptions);
                        
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
                    // DATA FLOW:
                    // SpriteItemCollection → JSON text
                    // This includes ALL sprites and their properties
                    // Using cached serialize options instead of creating new ones
                    string jsonContent = JsonSerializer.Serialize(_spriteCollection, SerializeOptions);
                    
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

        /// <summary>
        /// Handles the "Copy Current Sprite" button click.
        /// Serializes the currently selected sprite to JSON and copies it to the clipboard.
        /// 
        /// DATA FLOW:
        /// Selected SpriteItem → Serialized to JSON → Clipboard
        /// </summary>
        private void CopySpriteJson_Click(object sender, RoutedEventArgs e)
        {
            // Make sure we have a selected sprite
            if (_spriteCollection.SelectedItem != null)
            {
                try
                {
                    // DATA FLOW:
                    // SpriteItem → JSON text
                    // Using cached serialize options for consistent formatting
                    string jsonContent = JsonSerializer.Serialize(_spriteCollection.SelectedItem, SerializeOptions);
                    
                    // Copy the JSON to the clipboard
                    Clipboard.SetText(jsonContent);
                    
                    // Show success message with sprite name
                    MessageBox.Show($"Copied sprite '{_spriteCollection.SelectedItem.Name}' to clipboard!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // ERROR HANDLING:
                    // Show details about what went wrong during copying
                    MessageBox.Show($"Error copying JSON to clipboard: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No sprite selected to copy.", 
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the "Copy All Sprites" button click.
        /// Serializes the entire sprite collection to JSON and copies it to the clipboard.
        /// 
        /// DATA FLOW:
        /// SpriteItemCollection → Serialized to JSON → Clipboard
        /// </summary>
        private void CopyAllJson_Click(object sender, RoutedEventArgs e)
        {
            if (_spriteCollection.Items.Count > 0)
            {
                try
                {
                    // DATA FLOW:
                    // SpriteItemCollection → JSON text
                    // This includes ALL sprites and their properties
                    // Using cached serialize options for consistent formatting
                    string jsonContent = JsonSerializer.Serialize(_spriteCollection, SerializeOptions);
                    
                    // Copy the JSON to the clipboard
                    Clipboard.SetText(jsonContent);
                    
                    // Show success message with sprite count
                    MessageBox.Show($"Copied all {_spriteCollection.Items.Count} sprites to clipboard!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // ERROR HANDLING:
                    // Show details about what went wrong during copying
                    MessageBox.Show($"Error copying JSON to clipboard: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No sprites available to copy.", 
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}