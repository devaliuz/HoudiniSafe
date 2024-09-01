// RelayCommand.cs
// This file contains implementations for various command types used in MVVM pattern.
// Includes:
// - RelayCommand: For commands without parameters.
// - RelayCommand<T>: For commands with parameters.
// - AsyncRelayCommand: For asynchronous commands without parameters.
// - AsyncRelayCommand<T>: For asynchronous commands with parameters.

using System;
using System.Windows.Input;
using System.Threading.Tasks;

namespace HoudiniSafe.ViewModel.Commands
{
    /// <summary>
    /// Represents a command that can execute an action with no parameters.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">A function that determines whether the command can execute.</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">The parameter for the command (not used).</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">The parameter for the command (not used).</param>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// Occurs when the ability of the command to execute has changed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>
    /// Represents a command that can execute an action with a parameter.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand<T> class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">A predicate that determines whether the command can execute.</param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">The parameter for the command.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">The parameter for the command.</param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        /// <summary>
        /// Occurs when the ability of the command to execute has changed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>
    /// Represents an asynchronous command that can execute a task with no parameters.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the AsyncRelayCommand class.
        /// </summary>
        /// <param name="execute">The asynchronous task to execute.</param>
        /// <param name="canExecute">A function that determines whether the command can execute.</param>
        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">The parameter for the command (not used).</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        /// <summary>
        /// Executes the asynchronous command.
        /// </summary>
        /// <param name="parameter">The parameter for the command (not used).</param>
        public async void Execute(object parameter)
        {
            await _execute();
        }

        /// <summary>
        /// Occurs when the ability of the command to execute has changed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>
    /// Represents an asynchronous command that can execute a task with a parameter.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Predicate<T> _canExecute;

        /// <summary>
        /// Initializes a new instance of the AsyncRelayCommand<T> class.
        /// </summary>
        /// <param name="execute">The asynchronous task to execute.</param>
        /// <param name="canExecute">A predicate that determines whether the command can execute.</param>
        public AsyncRelayCommand(Func<T, Task> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">The parameter for the command.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        /// <summary>
        /// Executes the asynchronous command.
        /// </summary>
        /// <param name="parameter">The parameter for the command.</param>
        public async void Execute(object parameter)
        {
            await _execute((T)parameter);
        }

        /// <summary>
        /// Occurs when the ability of the command to execute has changed.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
