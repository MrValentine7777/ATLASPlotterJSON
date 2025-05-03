using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ATLASPlotterJSON.Commands
{
    /// <summary>
    /// Manages the history of commands for undo and redo operations.
    /// This class maintains two stacks: one for executed commands that can be undone,
    /// and another for undone commands that can be redone.
    /// </summary>
    public class CommandManager : INotifyPropertyChanged
    {
        private static CommandManager _instance;

        /// <summary>
        /// Gets the singleton instance of the CommandManager.
        /// </summary>
        public static CommandManager Instance => _instance ??= new CommandManager();

        /// <summary>
        /// Maximum number of commands to keep in history.
        /// This prevents memory issues with very long undo histories.
        /// </summary>
        private const int MaxHistorySize = 50;

        /// <summary>
        /// Stack of executed commands that can be undone.
        /// Most recent command is at the top of the stack.
        /// </summary>
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();

        /// <summary>
        /// Stack of undone commands that can be redone.
        /// Most recent undone command is at the top of the stack.
        /// </summary>
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        /// <summary>
        /// Event that fires when a command is executed, undone, or redone.
        /// This allows UI components to update their state based on history changes.
        /// </summary>
        public event EventHandler<ICommand> CommandStateChanged;

        /// <summary>
        /// Gets whether there are commands that can be undone.
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Gets whether there are commands that can be redone.
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Gets the name of the command that would be undone next.
        /// </summary>
        public string UndoCommandName => CanUndo ? _undoStack.Peek().Name : "Nothing to undo";

        /// <summary>
        /// Gets the name of the command that would be redone next.
        /// </summary>
        public string RedoCommandName => CanRedo ? _redoStack.Peek().Name : "Nothing to redo";

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private CommandManager() 
        {
            // Register keyboard shortcuts for undo/redo
            // This allows the application to respond to Ctrl+Z and Ctrl+Y
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, 
                (sender, e) => Undo(), 
                (sender, e) => e.CanExecute = CanUndo));
                
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, 
                (sender, e) => Redo(), 
                (sender, e) => e.CanExecute = CanRedo));
        }

        /// <summary>
        /// Collection of command bindings for keyboard shortcuts.
        /// </summary>
        public CommandBindingCollection CommandBindings { get; } = new CommandBindingCollection();

        /// <summary>
        /// Executes a command and adds it to the undo stack.
        /// Clears the redo stack since the command history has now branched.
        /// </summary>
        /// <param name="command">The command to execute</param>
        public void ExecuteCommand(ICommand command)
        {
            // Execute the command first
            command.Execute();
            
            // Add the command to the undo stack
            _undoStack.Push(command);
            
            // Clear the redo stack since we've created a new branch in history
            _redoStack.Clear();
            
            // Trim history if it gets too large
            if (_undoStack.Count > MaxHistorySize)
            {
                // Create a new stack with just the most recent MaxHistorySize commands
                var newStack = new Stack<ICommand>();
                var tempArray = _undoStack.ToArray();
                
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    newStack.Push(tempArray[i]);
                }
                
                _undoStack.Clear();
                foreach (var cmd in newStack)
                {
                    _undoStack.Push(cmd);
                }
            }
            
            // Notify listeners that command state has changed
            NotifyStateChange(command);
        }

        /// <summary>
        /// Undoes the most recently executed command.
        /// The command is moved from the undo stack to the redo stack.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;
            
            // Get the most recent command from the undo stack
            ICommand command = _undoStack.Pop();
            
            // Undo the command
            command.Undo();
            
            // Add the command to the redo stack
            _redoStack.Push(command);
            
            // Notify listeners that command state has changed
            NotifyStateChange(command);
        }

        /// <summary>
        /// Redoes the most recently undone command.
        /// The command is moved from the redo stack to the undo stack.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;
            
            // Get the most recent command from the redo stack
            ICommand command = _redoStack.Pop();
            
            // Redo the command
            command.Redo();
            
            // Add the command to the undo stack
            _undoStack.Push(command);
            
            // Notify listeners that command state has changed
            NotifyStateChange(command);
        }

        /// <summary>
        /// Clears all command history.
        /// This is typically done when loading a new file or resetting the application state.
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            
            // Notify listeners that command state has changed
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoCommandName));
            OnPropertyChanged(nameof(RedoCommandName));
            CommandStateChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Notifies listeners that command state has changed.
        /// </summary>
        /// <param name="command">The command that was executed, undone, or redone</param>
        private void NotifyStateChange(ICommand command)
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoCommandName));
            OnPropertyChanged(nameof(RedoCommandName));
            CommandStateChanged?.Invoke(this, command);
        }

        /// <summary>
        /// Event for property change notification to update UI bindings.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}