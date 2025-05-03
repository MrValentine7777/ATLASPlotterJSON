using System;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Command for adding a new sprite to the sprite collection.
    /// Implements undo by removing the added sprite.
    /// </summary>
    public class AddSpriteCommand : ICommand
    {
        private readonly SpriteItemCollection _spriteCollection;
        private SpriteItem _addedSprite;
        private readonly Action<SpriteItem> _onSpriteAdded;

        /// <summary>
        /// Gets the descriptive name of this command.
        /// </summary>
        public string Name => "Add Sprite";

        /// <summary>
        /// Creates a new AddSpriteCommand.
        /// </summary>
        /// <param name="spriteCollection">The collection to add the sprite to</param>
        /// <param name="onSpriteAdded">Callback to notify when a sprite is added</param>
        public AddSpriteCommand(SpriteItemCollection spriteCollection, Action<SpriteItem> onSpriteAdded)
        {
            _spriteCollection = spriteCollection;
            _onSpriteAdded = onSpriteAdded;
        }

        /// <summary>
        /// Executes the command, adding a new sprite to the collection.
        /// </summary>
        public void Execute()
        {
            // Create and add the sprite through the collection's method
            _addedSprite = _spriteCollection.AddNewItemInternal();
            
            // Notify listeners about the new sprite
            _onSpriteAdded?.Invoke(_addedSprite);
        }

        /// <summary>
        /// Undoes the command by removing the added sprite.
        /// </summary>
        public void Undo()
        {
            if (_addedSprite != null)
            {
                _spriteCollection.RemoveItemInternal(_addedSprite);
            }
        }

        /// <summary>
        /// Redoes the command by re-adding the previously added sprite.
        /// </summary>
        public void Redo()
        {
            if (_addedSprite != null)
            {
                _spriteCollection.AddExistingItemInternal(_addedSprite);
                _onSpriteAdded?.Invoke(_addedSprite);
            }
        }
    }
}