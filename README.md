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
  - Adjustable zoom levels with exact 1:1 pixel match option
  - Pixel grid overlay for precise pixel-level editing
  - Real-time update of sprite markers
  - Viewport indicator to show current view area
  - Pan navigation for moving around the zoomed image
- **Pixel-Perfect Positioning**: Coordinate tracking for precise sprite placement
  - Live coordinate display showing exact X,Y position
  - Visual highlight box showing current pixel selection
  - Snap-to-pixel functionality for accurate placement
- **Undo/Redo Support**: Track all operations with unlimited undo/redo capabilities
  - Keyboard shortcuts: Ctrl+Z for Undo, Ctrl+Y for Redo
  - All sprite modifications are tracked and reversible
  - Command history shows descriptive names of operations
- **Visual Editing**: Direct manipulation of sprites on the canvas
  - Color-coded sprite boundaries for easy identification
  - Visual selection and positioning of sprites
  - Real-time updates between visual editor and property controls

## Technical Details
- Built with .NET 9.0 for Windows
- Uses WPF (Windows Presentation Foundation) for the user interface
- Implements MVVM-like architecture with two-way data binding
- Serializes/deserializes sprite data using System.Text.Json
- Uses Command Pattern for operation tracking and undo/redo functionality
- Pixel-perfect rendering using NearestNeighbor scaling mode

## Sprite Data Structure
The application uses a comprehensive data model for sprites:
- **SpriteItem**: The core sprite class containing all sprite properties
  - Basic properties: ID, name, Y-sort order, fragile and breakable flags
  - Source rectangle: X, Y, Width, Height defining the sprite's position in the atlas
  - Offset: Position adjustment for in-game rendering
  - Shadow properties: Position and source rectangle for sprite shadows
  - Colliders: Collection of collision boundaries for game physics
  - Breaking Animation: Animation data for when a sprite is destroyed

## Application Architecture
- **MainWindow**: The UI container and main controller
- **AtlasImage**: Represents the sprite atlas image and its properties
- **SpriteItem**: Represents individual sprites within the atlas, including properties like position, size, and name
- **SpriteItemCollection**: Manages the collection of sprites and their properties, including selection and color assignment
- **SpriteItemMarker**: Visual representation of sprites on the canvas with interaction capabilities
- **Collider**: Represents the collision boundaries for sprites
- **JsonDataEntryControl**: Provides the UI for editing sprite properties and managing sprites
- **JsonDataModels**: Defines the data structure for sprites, colliders, and animations
- **ZoomViewer**: Provides a zoomed-in view of the sprite atlas for detailed editing, including pixel grid overlay
- **PixelLocationDisplay**: Tracks and displays pixel coordinates for precise sprite placement
- **CommandManager**: Manages the history of operations for undo/redo functionality
- **Commands**: Implements the Command Pattern for various sprite operations
  - **ICommand**: Base interface for all commands
  - **AddSpriteCommand**: Handles adding new sprites
  - **RemoveSpriteCommand**: Handles removing sprites
  - **ModifySpriteCommand**: Handles changes to sprite properties
  - **AddColliderCommand**: Handles adding colliders to sprites
  - **RemoveColliderCommand**: Handles removing colliders from sprites

## Command Pattern Implementation
The application uses the Command Pattern to implement undo/redo functionality:
- Each user action (add sprite, remove sprite, modify property) is encapsulated as a command
- Commands are stored in history stacks (undo and redo) managed by CommandManager
- Each command knows how to execute, undo, and redo itself
- Commands maintain the state necessary to reverse operations
- The CommandManager provides a centralized system for tracking all changes
- UI components subscribe to CommandManager's state change events to update buttons and tooltips

## Event Communication System
The application uses a sophisticated event system for communication between components:
- SpriteItemCollection notifies listeners when sprite selection changes
- JsonDataEntryControl notifies MainWindow when sprites are added, removed or modified
- SpriteItems implement INotifyPropertyChanged for two-way data binding with the UI
- MainWindow coordinates visual updates based on data changes
- The ZoomViewer synchronizes with the main canvas view through events

## Made with the help of GitHub Copilot
- **GitHub Copilot**: Assisted in generating code snippets, comments, and documentation