using System;
using System.Collections.ObjectModel;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Command for removing a collider from a sprite.
    /// Implements undo by re-adding the removed collider.
    /// </summary>
    public class RemoveColliderCommand : ICommand
    {
        private readonly SpriteItem _sprite;
        private readonly Collider _collider;
        private readonly int _originalIndex;

        /// <summary>
        /// Gets the descriptive name of this command.
        /// </summary>
        public string Name => $"Remove Collider from '{_sprite.Name}'";

        /// <summary>
        /// Creates a new RemoveColliderCommand.
        /// </summary>
        /// <param name="sprite">The sprite to remove the collider from</param>
        /// <param name="collider">The collider to remove</param>
        public RemoveColliderCommand(SpriteItem sprite, Collider collider)
        {
            _sprite = sprite;
            _collider = collider;
            
            // Store the original index for reinsertion during undo
            _originalIndex = _sprite.Colliders.IndexOf(collider);
        }

        /// <summary>
        /// Executes the command, removing the collider from the sprite.
        /// </summary>
        public void Execute()
        {
            _sprite.Colliders.Remove(_collider);
        }

        /// <summary>
        /// Undoes the command by re-adding the removed collider.
        /// </summary>
        public void Undo()
        {
            // Re-add at the original position if possible
            if (_originalIndex >= 0 && _originalIndex <= _sprite.Colliders.Count)
            {
                _sprite.Colliders.Insert(_originalIndex, _collider);
            }
            else
            {
                // Otherwise, add at the end
                _sprite.Colliders.Add(_collider);
            }
        }

        /// <summary>
        /// Redoes the command by removing the collider again.
        /// </summary>
        public void Redo() => Execute();
    }
}