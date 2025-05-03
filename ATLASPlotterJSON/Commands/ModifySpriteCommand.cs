using System;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Command for modifying a sprite's properties.
    /// Implements undo by restoring the sprite's previous state.
    /// </summary>
    public class ModifySpriteCommand : ICommand
    {
        private readonly SpriteItem _sprite;
        private readonly string _propertyName;
        private readonly object _oldValue;
        private readonly object _newValue;
        private readonly Action<SpriteItem> _onSpriteModified;

        /// <summary>
        /// Gets the descriptive name of this command.
        /// </summary>
        public string Name => $"Modify Sprite '{_sprite.Name}' {_propertyName}";

        /// <summary>
        /// Creates a new ModifySpriteCommand.
        /// </summary>
        /// <param name="sprite">The sprite to modify</param>
        /// <param name="propertyName">Name of the property being modified</param>
        /// <param name="oldValue">The property's original value</param>
        /// <param name="newValue">The property's new value</param>
        /// <param name="onSpriteModified">Callback to notify when a sprite is modified</param>
        public ModifySpriteCommand(
            SpriteItem sprite, 
            string propertyName, 
            object oldValue, 
            object newValue,
            Action<SpriteItem> onSpriteModified)
        {
            _sprite = sprite;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
            _onSpriteModified = onSpriteModified;
        }

        /// <summary>
        /// Executes the command, applying the new property value.
        /// </summary>
        public void Execute()
        {
            // Set the property to the new value using reflection
            SetPropertyValue(_sprite, _propertyName, _newValue);
            _onSpriteModified?.Invoke(_sprite);
        }

        /// <summary>
        /// Undoes the command by restoring the previous property value.
        /// </summary>
        public void Undo()
        {
            // Restore the property to its old value using reflection
            SetPropertyValue(_sprite, _propertyName, _oldValue);
            _onSpriteModified?.Invoke(_sprite);
        }

        /// <summary>
        /// Redoes the command by applying the new property value again.
        /// </summary>
        public void Redo() => Execute();

        /// <summary>
        /// Sets a property value using reflection.
        /// Handles nested properties using dot notation (e.g., "Source.X").
        /// </summary>
        /// <param name="target">The object to modify</param>
        /// <param name="propertyPath">Path to the property (can be nested with dots)</param>
        /// <param name="value">The value to set</param>
        private void SetPropertyValue(object target, string propertyPath, object value)
        {
            // Handle nested properties (e.g., "Source.X")
            if (propertyPath.Contains("."))
            {
                var parts = propertyPath.Split('.');
                var property = target.GetType().GetProperty(parts[0]);
                var nestedTarget = property.GetValue(target);
                
                // Get the rest of the property path
                var nestedPath = propertyPath[(parts[0].Length + 1)..];
                
                // Recursively set the nested property
                SetPropertyValue(nestedTarget, nestedPath, value);
            }
            else
            {
                // Set the property directly
                var property = target.GetType().GetProperty(propertyPath);
                if (property != null)
                {
                    property.SetValue(target, value);
                }
            }
        }
    }
}