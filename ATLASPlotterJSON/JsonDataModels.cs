using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ATLASPlotterJSON
{
    /// <summary>
    /// DATA MODEL - CORE SPRITE CLASS
    /// 
    /// Represents a single sprite within a sprite atlas.
    /// A sprite is an individual image element that can be drawn in a game or application.
    /// This class stores all information needed to locate, display and interact with a sprite.
    /// 
    /// PROJECT ARCHITECTURE:
    /// - SpriteItem objects are created in the JsonDataEntryControl
    /// - They are visually represented as SpriteItemMarkers on the canvas
    /// - They are stored in the SpriteItemCollection for organization
    /// - They are serialized to JSON when saving the project
    /// 
    /// The SpriteItem implements INotifyPropertyChanged to support WPF data binding,
    /// which automatically updates the UI when properties change.
    /// </summary>
    public class SpriteItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Unique identifier for this sprite.
        /// Each sprite must have a different ID to distinguish it from others.
        /// Used by SpriteItemCollection to track sprites and by SpriteMarkers for display.
        /// </summary>
        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Display name of the sprite.
        /// This is shown in the sprite list and in the marker labels on the canvas.
        /// Helps users identify sprites when working with multiple sprites.
        /// </summary>
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Y-sorting order for the sprite.
        /// Used in games to determine which sprites appear in front of or behind others.
        /// Higher values make sprites appear in front of sprites with lower values.
        /// </summary>
        private int _ySort;
        public int YSort
        {
            get => _ySort;
            set { _ySort = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Indicates if the sprite is fragile and can be affected by game events.
        /// Used in game logic to determine sprite behavior.
        /// </summary>
        private bool _fragile;
        public bool Fragile
        {
            get => _fragile;
            set { _fragile = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Indicates if the sprite can be broken by game events.
        /// When true, the BreakingAnimation will be used when the sprite breaks.
        /// </summary>
        private bool _breakable;
        public bool Breakable
        {
            get => _breakable;
            set { _breakable = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Position offset for displaying the sprite in-game.
        /// This adjusts where the sprite is drawn relative to its logical position.
        /// </summary>
        private PointOffset _offset = new();
        public PointOffset Offset
        {
            get => _offset;
            set { _offset = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Rectangle defining where in the atlas image this sprite is located.
        /// This is the key information that identifies which pixels belong to this sprite.
        /// The SpriteItemMarker visualizes this rectangle on the canvas.
        /// </summary>
        private RectSource _source = new();
        public RectSource Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Position offset for the sprite's shadow in-game.
        /// If the sprite has a shadow, this defines where it appears relative to the sprite.
        /// </summary>
        private PointOffset _shadowOffset = new();
        public PointOffset ShadowOffset
        {
            get => _shadowOffset;
            set { _shadowOffset = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Rectangle defining where in the atlas image the sprite's shadow is located.
        /// Similar to Source, but for the shadow graphic that accompanies the sprite.
        /// </summary>
        private RectSource _shadowSource = new();
        public RectSource ShadowSource
        {
            get => _shadowSource;
            set { _shadowSource = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List of collision areas associated with this sprite.
        /// These define which parts of the sprite can interact with other game elements.
        /// For example, a tree might have a smaller collision area than its visual appearance.
        /// </summary>
        private ObservableCollection<Collider> _colliders = new();
        public ObservableCollection<Collider> Colliders
        {
            get => _colliders;
            set { _colliders = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Animation information for when the sprite breaks.
        /// Used for breakable sprites to define how they animate when destroyed.
        /// </summary>
        private BreakingAnimation _breakingAnimation = new();
        public BreakingAnimation BreakingAnimation
        {
            get => _breakingAnimation;
            set { _breakingAnimation = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// EVENT SYSTEM - PROPERTY CHANGE NOTIFICATION
        /// 
        /// This event is part of the INotifyPropertyChanged interface.
        /// It notifies UI elements when properties in this class change.
        /// 
        /// HOW IT WORKS:
        /// 1. UI elements (like textboxes) bind to these properties
        /// 2. When a property changes, this event fires
        /// 3. The UI automatically updates to show the new value
        /// 
        /// This creates a two-way connection:
        /// - Changes in the UI update the property values
        /// - Changes in the property values update the UI
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event to notify the UI that a property value has changed.
        /// The [CallerMemberName] attribute automatically provides the property name,
        /// so you don't need to specify which property changed - the compiler figures it out.
        /// </summary>
        /// <param name="name">Name of the property that changed</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// DATA MODEL - POINT POSITION
    /// 
    /// Represents a 2D point with X and Y coordinates.
    /// Used for offsets and positions throughout the sprite data.
    /// 
    /// This is used in two ways:
    /// 1. As an offset for displaying sprites (Offset, ShadowOffset)
    /// 2. As a position within the sprite atlas (used indirectly)
    /// 
    /// Like other classes, it implements INotifyPropertyChanged for data binding.
    /// </summary>
    public class PointOffset : INotifyPropertyChanged
    {
        /// <summary>
        /// X coordinate (horizontal position)
        /// Positive values move to the right, negative to the left
        /// </summary>
        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Y coordinate (vertical position)
        /// Positive values move down, negative values move up
        /// </summary>
        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// EVENT SYSTEM - PROPERTY CHANGE NOTIFICATION
        /// 
        /// See the comments in SpriteItem for detailed explanation of how this works.
        /// This enables automatic UI updates when X or Y values change.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// DATA MODEL - RECTANGLE DEFINITION
    /// 
    /// Defines a rectangular area with X, Y, Width, and Height.
    /// Used to specify areas within the sprite atlas image.
    /// 
    /// This is primarily used for:
    /// 1. Defining which pixels in the atlas make up a sprite (Source)
    /// 2. Defining shadow areas (ShadowSource)
    /// 3. Defining animation frames (BreakingAnimation.Source)
    /// 
    /// The UI displays this as a colored rectangle on the canvas.
    /// </summary>
    public class RectSource : INotifyPropertyChanged
    {
        /// <summary>
        /// X position of the rectangle's top-left corner in the atlas
        /// This is set when a user clicks on the atlas image
        /// </summary>
        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Y position of the rectangle's top-left corner in the atlas
        /// This is set when a user clicks on the atlas image
        /// </summary>
        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Width of the rectangle in pixels
        /// This determines how many pixels wide the sprite is
        /// </summary>
        private int _width;
        public int Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Height of the rectangle in pixels
        /// This determines how many pixels tall the sprite is
        /// </summary>
        private int _height;
        public int Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// EVENT SYSTEM - PROPERTY CHANGE NOTIFICATION
        /// 
        /// See the comments in SpriteItem for detailed explanation of how this works.
        /// This enables automatic UI updates when position or size values change.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// DATA MODEL - COLLISION AREA
    /// 
    /// Defines a collision area for a sprite.
    /// Colliders determine which parts of a sprite can interact with other game elements.
    /// 
    /// GAME DEVELOPMENT CONCEPT:
    /// Often, the visual appearance of a sprite is larger than its actual interactive area.
    /// For example, a tree might have a small collision box at its base, even though
    /// the tree graphic is much larger. This allows players to walk "behind" the tree.
    /// </summary>
    public class Collider : INotifyPropertyChanged
    {
        /// <summary>
        /// Type of collision shape.
        /// Currently only "rectangle" is supported, but could be extended to support
        /// other shapes like "circle" or "polygon" in the future.
        /// </summary>
        private string _type = "rectangle";
        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// X position of the collider relative to the sprite's top-left corner
        /// This allows placing the collision area anywhere within or around the sprite
        /// </summary>
        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Y position of the collider relative to the sprite's top-left corner
        /// This allows placing the collision area anywhere within or around the sprite
        /// </summary>
        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Width of the collision area in pixels
        /// Can be smaller or larger than the sprite itself
        /// </summary>
        private int _width;
        public int Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Height of the collision area in pixels
        /// Can be smaller or larger than the sprite itself
        /// </summary>
        private int _height;
        public int Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// EVENT SYSTEM - PROPERTY CHANGE NOTIFICATION
        /// 
        /// See the comments in SpriteItem for detailed explanation of how this works.
        /// This enables automatic UI updates when collider properties change.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// DATA MODEL - BREAKING ANIMATION
    /// 
    /// Defines the animation that plays when a breakable sprite is destroyed.
    /// This contains all the information needed to display the breaking sequence.
    /// 
    /// GAME DEVELOPMENT CONCEPT:
    /// Sprite animations are typically created as a series of frames in a strip or grid.
    /// Each frame shows the sprite in a slightly different state, and displaying them
    /// in sequence creates the illusion of movement or changing state.
    /// </summary>
    public class BreakingAnimation : INotifyPropertyChanged
    {
        /// <summary>
        /// Rectangle defining the location of the first animation frame in the atlas
        /// The animation frames are assumed to be arranged horizontally from this position
        /// </summary>
        private RectSource _source = new();
        public RectSource Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Position offset for drawing the breaking animation
        /// This can be different from the sprite's normal offset to accommodate
        /// animation frames that are larger than the original sprite
        /// </summary>
        private PointOffset _offset = new();
        public PointOffset Offset
        {
            get => _offset;
            set { _offset = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// X-axis inversion value for mirrored breaking animations
        /// This allows the same animation to be flipped horizontally
        /// </summary>
        private int _xInverted;
        public int XInverted
        {
            get => _xInverted;
            set { _xInverted = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Duration of each animation frame in milliseconds
        /// Controls how fast the breaking animation plays
        /// </summary>
        private int _frameDuration;
        public int FrameDuration
        {
            get => _frameDuration;
            set { _frameDuration = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Number of frames in the breaking animation
        /// The animation system uses this to know how many frames to display
        /// </summary>
        private int _nbFrames;
        public int NbFrames
        {
            get => _nbFrames;
            set { _nbFrames = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// EVENT SYSTEM - PROPERTY CHANGE NOTIFICATION
        /// 
        /// See the comments in SpriteItem for detailed explanation of how this works.
        /// This enables automatic UI updates when animation properties change.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}