using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ATLASPlotterJSON
{
    public class SpriteItemCollection : INotifyPropertyChanged
    {
        private ObservableCollection<SpriteItem> _items = new ObservableCollection<SpriteItem>();
        public ObservableCollection<SpriteItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        private SpriteItem _selectedItem;
        public SpriteItem SelectedItem
        {
            get => _selectedItem;
            set 
            { 
                _selectedItem = value; 
                OnPropertyChanged();
                OnSelectedItemChanged?.Invoke(this, _selectedItem);
            }
        }

        // Dictionary to track sprite item colors for visual identification
        private Dictionary<int, Color> _itemColors = new Dictionary<int, Color>();
        private Random _random = new Random();

        public event EventHandler<SpriteItem> OnSelectedItemChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public SpriteItemCollection()
        {
            // Initialize with one default item
            AddNewItem();
        }

        public void AddNewItem()
        {
            // Create a new sprite item with default values
            var newItem = CreateDefaultSpriteItem();
            
            // Assign a color for visual identification
            AssignRandomColor(newItem.Id);
            
            Items.Add(newItem);
            SelectedItem = newItem; // Select the new item
        }

        public void RemoveItem(SpriteItem item)
        {
            if (Items.Contains(item))
            {
                // Remove the item's color
                if (_itemColors.ContainsKey(item.Id))
                    _itemColors.Remove(item.Id);
                
                Items.Remove(item);
                
                // If we removed the selected item, select another one if available
                if (SelectedItem == item)
                {
                    SelectedItem = Items.Count > 0 ? Items[0] : null;
                }
            }
        }

        public Color GetItemColor(int id)
        {
            if (_itemColors.ContainsKey(id))
                return _itemColors[id];
            
            return Colors.Red; // Default color
        }

        private void AssignRandomColor(int id)
        {
            // Generate a distinct, bright color for this sprite item
            Color newColor = Color.FromRgb(
                (byte)_random.Next(100, 255),
                (byte)_random.Next(100, 255),
                (byte)_random.Next(100, 255));
            
            _itemColors[id] = newColor;
        }

        private SpriteItem CreateDefaultSpriteItem()
        {
            // Create an example sprite item with sample data
            var sprite = new SpriteItem
            {
                Id = GenerateUniqueId(),
                Name = $"sprite_{DateTime.Now.Ticks % 10000}",
                YSort = 10,
                Fragile = true,
                Breakable = true,
                Offset = new PointOffset { X = 3, Y = 3 },
                Source = new RectSource { X = 0, Y = 0, Width = 10, Height = 9 },
                ShadowOffset = new PointOffset { X = 4, Y = 8 },
                ShadowSource = new RectSource { X = 1, Y = 18, Width = 8, Height = 6 },
                Colliders = new List<Collider>
                {
                    new Collider { Type = "rectangle", X = 4, Y = 4, Width = 8, Height = 8 }
                },
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

        private int GenerateUniqueId()
        {
            // Simple ID generation - find the highest current ID and add 1
            int maxId = 1000;
            foreach (var item in Items)
            {
                if (item.Id > maxId)
                    maxId = item.Id;
            }
            return maxId + 1;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}