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
    /// </summary>
    public partial class JsonDataEntryControl : UserControl
    {
        // Collection of sprite items
        private SpriteItemCollection _spriteCollection = new SpriteItemCollection();
        
        // Event to notify when selected sprite item has changed
        public event EventHandler<SpriteItem> SelectedSpriteChanged;
        
        // Events for adding and removing sprites
        public event EventHandler<SpriteItem> SpriteAdded;
        public event EventHandler<SpriteItem> SpriteRemoved;

        public SpriteItemCollection SpriteCollection => _spriteCollection;

        public JsonDataEntryControl()
        {
            InitializeComponent();
            
            // Set the DataContext to our sprite collection
            this.DataContext = _spriteCollection;
            
            // Subscribe to selection change events
            _spriteCollection.OnSelectedItemChanged += (s, item) => 
            {
                SelectedSpriteChanged?.Invoke(this, item);
            };
        }

        public void SetCurrentLocation(Point location)
        {
            if (_spriteCollection?.SelectedItem != null)
            {
                // Update the source with the current location for the selected sprite
                _spriteCollection.SelectedItem.Source.X = (int)location.X;
                _spriteCollection.SelectedItem.Source.Y = (int)location.Y;
            }
        }

        private void AddCollider_Click(object sender, RoutedEventArgs e)
        {
            if (_spriteCollection.SelectedItem != null)
            {
                _spriteCollection.SelectedItem.Colliders.Add(new Collider
                {
                    Type = "rectangle",
                    X = 0,
                    Y = 0,
                    Width = 8,
                    Height = 8
                });
            }
        }

        private void RemoveCollider_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Collider collider && 
                _spriteCollection.SelectedItem != null)
            {
                _spriteCollection.SelectedItem.Colliders.Remove(collider);
            }
        }
        
        private void AddSprite_Click(object sender, RoutedEventArgs e)
        {
            _spriteCollection.AddNewItem();
            SpriteAdded?.Invoke(this, _spriteCollection.SelectedItem);
        }
        
        private void RemoveSprite_Click(object sender, RoutedEventArgs e)
        {
            if (_spriteCollection.SelectedItem != null)
            {
                var itemToRemove = _spriteCollection.SelectedItem;
                _spriteCollection.RemoveItem(itemToRemove);
                SpriteRemoved?.Invoke(this, itemToRemove);
            }
        }

        private void LoadJson_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Load JSON Data",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonContent = File.ReadAllText(openFileDialog.FileName);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    // Check if this is a single sprite or a collection
                    if (jsonContent.Contains("\"Items\":"))
                    {
                        // Try to deserialize as a collection
                        SpriteItemCollection? loadedCollection = JsonSerializer.Deserialize<SpriteItemCollection>(jsonContent, options);
                        
                        if (loadedCollection != null)
                        {
                            _spriteCollection = loadedCollection;
                            this.DataContext = _spriteCollection;
                            
                            // Notify about loaded sprites
                            foreach (var sprite in _spriteCollection.Items)
                            {
                                SpriteAdded?.Invoke(this, sprite);
                            }
                            
                            MessageBox.Show($"Loaded {_spriteCollection.Items.Count} sprite items.", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        // Try to deserialize as a single sprite
                        SpriteItem? loadedItem = JsonSerializer.Deserialize<SpriteItem>(jsonContent, options);
                        
                        if (loadedItem != null)
                        {
                            // Create a new collection with just this item
                            _spriteCollection = new SpriteItemCollection();
                            _spriteCollection.Items.Clear();
                            _spriteCollection.Items.Add(loadedItem);
                            _spriteCollection.SelectedItem = loadedItem;
                            this.DataContext = _spriteCollection;
                            
                            SpriteAdded?.Invoke(this, loadedItem);
                            
                            MessageBox.Show("Single sprite item loaded successfully!", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading JSON: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveJson_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save JSON Data",
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    // Save the entire collection
                    string jsonContent = JsonSerializer.Serialize(_spriteCollection, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonContent);
                    
                    MessageBox.Show($"JSON data saved successfully with {_spriteCollection.Items.Count} sprites!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving JSON: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}