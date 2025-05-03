# ATLAS Plotter JSON

A WPF application for defining and managing sprite regions within sprite atlas images for game development.

## Overview
ATLAS Plotter is a tool for defining sprite regions within a sprite atlas image. A sprite atlas is a single image containing multiple individual graphics, commonly used in game development to improve rendering efficiency and performance.

## Features
- **Atlas Image Management**: Load and display sprite atlas images
- **Sprite Definition**: Select and define individual sprites within the atlas
- **Property Configuration**: Configure sprite properties (position, size, name, ID)
- **Collider Support**: Add collision boundaries to sprites for game physics
- **JSON Import/Export**: Save sprite data as structured JSON for use in game engines
- **Clipboard Support**: Copy sprite data directly to clipboard for quick sharing or integration
  - Copy the current selected sprite as formatted JSON
  - Copy all sprites as a complete JSON collection
- **Zoom Viewer**: Magnified view for detailed sprite editing and positioning
- **Pixel-Perfect Positioning**: Coordinate tracking for precise sprite placement

## Technical Details
- Built with .NET 9.0 for Windows
- Uses WPF (Windows Presentation Foundation) for the user interface
- Implements MVVM-like architecture with two-way data binding
- Serializes/deserializes sprite data using System.Text.Json

## Application Architecture
- **MainWindow**: The UI container and main controller
- **AtlasImage**: Represents the sprite atlas image and its properties
- **Sprite**: Represents individual sprites within the atlas, including properties like position, size, and name
- **Collider**: Represents the collision boundaries for sprites
- **SpriteManager**: Manages the collection of sprites and their properties
- **JsonSerializer**: Handles serialization and deserialization of sprite data to/from JSON format
- **ZoomViewer**: Provides a zoomed-in view of the sprite atlas for detailed editing
- **PixelTracker**: Tracks pixel coordinates for precise sprite placement
- **SpriteSelector**: Allows users to select and define sprites within the atlas image
- **PropertyEditor**: Provides a UI for editing sprite properties
- **ColliderEditor**: Provides a UI for editing sprite colliders
- **FileManager**: Handles loading and saving of sprite atlas images and JSON data
- **ErrorHandler**: Manages error handling and user notifications


## Made with the help of GitHub CoPilot.
- **GitHub CoPilot**: Assisted in generating code snippets, comments, and documentation