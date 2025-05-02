using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ATLASPlotterJSON
{
    public class SpriteItem : INotifyPropertyChanged
    {
        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private int _ySort;
        public int YSort
        {
            get => _ySort;
            set { _ySort = value; OnPropertyChanged(); }
        }

        private bool _fragile;
        public bool Fragile
        {
            get => _fragile;
            set { _fragile = value; OnPropertyChanged(); }
        }

        private bool _breakable;
        public bool Breakable
        {
            get => _breakable;
            set { _breakable = value; OnPropertyChanged(); }
        }

        private PointOffset _offset = new();
        public PointOffset Offset
        {
            get => _offset;
            set { _offset = value; OnPropertyChanged(); }
        }

        private RectSource _source = new();
        public RectSource Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(); }
        }

        private PointOffset _shadowOffset = new();
        public PointOffset ShadowOffset
        {
            get => _shadowOffset;
            set { _shadowOffset = value; OnPropertyChanged(); }
        }

        private RectSource _shadowSource = new();
        public RectSource ShadowSource
        {
            get => _shadowSource;
            set { _shadowSource = value; OnPropertyChanged(); }
        }

        private List<Collider> _colliders = new();
        public List<Collider> Colliders
        {
            get => _colliders;
            set { _colliders = value; OnPropertyChanged(); }
        }

        private BreakingAnimation _breakingAnimation = new();
        public BreakingAnimation BreakingAnimation
        {
            get => _breakingAnimation;
            set { _breakingAnimation = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class PointOffset : INotifyPropertyChanged
    {
        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RectSource : INotifyPropertyChanged
    {
        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        private int _width;
        public int Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        private int _height;
        public int Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Collider : INotifyPropertyChanged
    {
        private string _type = "rectangle";
        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        private int _width;
        public int Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        private int _height;
        public int Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class BreakingAnimation : INotifyPropertyChanged
    {
        private RectSource _source = new();
        public RectSource Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(); }
        }

        private PointOffset _offset = new();
        public PointOffset Offset
        {
            get => _offset;
            set { _offset = value; OnPropertyChanged(); }
        }

        private int _xInverted;
        public int XInverted
        {
            get => _xInverted;
            set { _xInverted = value; OnPropertyChanged(); }
        }

        private int _frameDuration;
        public int FrameDuration
        {
            get => _frameDuration;
            set { _frameDuration = value; OnPropertyChanged(); }
        }

        private int _nbFrames;
        public int NbFrames
        {
            get => _nbFrames;
            set { _nbFrames = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}