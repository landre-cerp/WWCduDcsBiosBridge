using System.Windows.Input;

namespace WWCduDcsBiosBridge.UI;

/// <summary>
/// A simple ICommand implementation for XAML commanding.
/// Note: The CanExecute predicate should be lightweight as it's called frequently by WPF's commanding system.
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => 
        _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => 
        _execute((T?)parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
