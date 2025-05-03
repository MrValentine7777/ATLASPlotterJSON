using System;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Interface for implementing the Command pattern.
    /// Commands represent actions that can be executed, undone, and redone.
    /// This enables the application to track user operations for undo/redo functionality.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets a descriptive name of the command for display in UI or logging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the command, performing the action it represents.
        /// </summary>
        void Execute();

        /// <summary>
        /// Undoes the command, reversing its effect.
        /// </summary>
        void Undo();

        /// <summary>
        /// Re-executes the command after it has been undone.
        /// </summary>
        void Redo();
    }
}