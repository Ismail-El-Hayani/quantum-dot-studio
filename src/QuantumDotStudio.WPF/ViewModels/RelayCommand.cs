using System.Windows.Input;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// Minimaler ICommand-Wrapper für MVVM-Binding.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?> _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?> canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
    }

    public bool CanExecute(object? parameter) => _canExecute(parameter);

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
