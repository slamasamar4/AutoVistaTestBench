using System.Windows.Input;

namespace AutoVistaTestBench.Infrastructure
{
    /// <summary>
    /// A standard ICommand implementation for MVVM command binding.
    /// Wraps Action delegates and optional CanExecute predicates.
    /// 
    /// Note: CommunityToolkit.Mvvm provides [RelayCommand] source generator,
    /// but this explicit implementation is included for educational clarity.
    /// Both approaches are used in industry; the CommunityToolkit version
    /// reduces boilerplate in larger projects.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
        {
        }

        /// <summary>
        /// Raised to notify the UI that command availability has changed.
        /// WPF automatically re-queries CanExecute when CommandManager detects UI changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>Manually triggers a CanExecute re-evaluation on the UI thread.</summary>
        public static void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Typed version of RelayCommand for parameter-carrying commands.</summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter is T t ? t : default) ?? true;

        public void Execute(object? parameter) =>
            _execute(parameter is T t ? t : default);
    }
}