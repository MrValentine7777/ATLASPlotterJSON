using System;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Command for removing a sprite from the sprite collection.
    /// Implements undo by re-adding the removed sprite.
    /// </summary>
    public class RemoveSpriteCommand : ICommand
    {
        private readonly SpriteItemCollection _spriteCollection;
        private readonly SpriteItem _spriteToRemove;
        private readonly Action<SpriteItem> _onSpriteRemoved;
        private readonly Action<SpriteItem> _onSpriteAdded;
        private int _originalIndex;
        private SpriteItem _previousSelection;

        /// <summary>
        /// Gets the descriptive name of this command.
        /// </summary>
        public string Name => $"Remove Sprite '{_spriteToRemove.Name}'";

        /// <summary>
        /// Creates a new RemoveSpriteCommand.
        /// </summary>
        /// <param name="spriteCollection">The collection to remove the sprite from</param>
        /// <param name="spriteToRemove">The sprite to remove</param>
        /// <param name="onSpriteRemoved">Callback to notify when a sprite is removed</param>
        /// <param name="onSpriteAdded">Callback to notify when a sprite is added (for undo)</param>
        public RemoveSpriteCommand(
            SpriteItemCollection spriteCollection, 
            SpriteItem spriteToRemove, 
            Action<SpriteItem> onSpriteRemoved,
            Action<SpriteItem> onSpriteAdded)
        {
            _spriteCollection = spriteCollection;
            _spriteToRemove = spriteToRemove;
            _onSpriteRemoved = onSpriteRemoved;
            _onSpriteAdded = onSpriteAdded;
            
            // Store the original index for reinsertion during undo
            _originalIndex = _spriteCollection.Items.IndexOf(_spriteToRemove);
            
            // Store the currently selected item for state restoration
            _previousSelection = _spriteCollection.SelectedItem;
        }

        /// <summary>
        /// Executes the command, removing the sprite from the collection.
        /// </summary>
        public void Execute()
        {
            _spriteCollection.RemoveItemInternal(_spriteToRemove);
            _onSpriteRemoved?.Invoke(_spriteToRemove);
        }

        /// <summary>
        /// Undoes the command by re-adding the removed sprite.
        /// </summary>
        public void Undo()
        {
            // Re-add the sprite at its original position if possible
            _spriteCollection.AddExistingItemInternal(_spriteToRemove, _originalIndex);
            
            // Restore the selection state
            if (_previousSelection == _spriteToRemove)
            {
                _spriteCollection.SelectedItem = _spriteToRemove;
            }
            
            // Notify listeners about the re-added sprite
            _onSpriteAdded?.Invoke(_spriteToRemove);
        }

        /// <summary>
        /// Redoes the command by removing the sprite again.
        /// </summary>
        public void Redo() => Execute();
    }
}