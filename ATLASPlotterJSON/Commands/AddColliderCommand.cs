using System;
using System.Collections.ObjectModel;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Command for adding a collider to a sprite.
    /// Implements undo by removing the added collider.
    /// </summary>
    public class AddColliderCommand : ICommand
    {
        private readonly SpriteItem _sprite;
        private readonly Collider _collider;

        /// <summary>
        /// Gets the descriptive name of this command.
        /// </summary>
        public string Name => $"Add Collider to '{_sprite.Name}'";

        /// <summary>
        /// Creates a new AddColliderCommand.
        /// </summary>
        /// <param name="sprite">The sprite to add the collider to</param>
        /// <param name="collider">The collider to add</param>
        public AddColliderCommand(SpriteItem sprite, Collider collider)
        {
            _sprite = sprite;
            _collider = collider;
        }

        /// <summary>
        /// Executes the command, adding the collider to the sprite.
        /// </summary>
        public void Execute()
        {
            _sprite.Colliders.Add(_collider);
        }

        /// <summary>
        /// Undoes the command by removing the added collider.
        /// </summary>
        public void Undo()
        {
            _sprite.Colliders.Remove(_collider);
        }

        /// <summary>
        /// Redoes the command by re-adding the collider.
        /// </summary>
        public void Redo() => Execute();
    }
}