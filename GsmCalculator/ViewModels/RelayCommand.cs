using System.Windows.Input;

namespace GsmCalculator.ViewModels;

/// <summary>
/// Универсальная реализация ICommand без параметра.
/// Используется для кнопок, которым не нужно ничего передавать
/// (например, "Открыть настройки", "Очистить дисплей").
///
/// Использование во ViewModel:
///   public ICommand SaveCommand { get; }
///   ...
///   SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
/// В XAML:
///   &lt;Button Command="{Binding SaveCommand}" Content="Сохранить"/&gt;
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// WPF слушает это событие чтобы пере-проверить CanExecute
    /// и автоматически делать кнопку enabled/disabled.
    /// CommandManager.RequerySuggested делает это автоматически
    /// при изменении фокуса / ввода и т.п.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Принудительно попросить WPF перепроверить CanExecute
    /// (когда меняется состояние ViewModel, влияющее на доступность команды).
    /// </summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}

/// <summary>
/// Типизированный вариант RelayCommand с параметром.
/// Используется когда из XAML нужно передать значение, например:
///   &lt;Button Command="{Binding DigitCommand}" CommandParameter="7" Content="7"/&gt;
/// и во ViewModel:
///   public ICommand DigitCommand { get; }
///   DigitCommand = new RelayCommand&lt;string&gt;(d => AppendDigit(d));
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        if (_canExecute is null) return true;
        return _canExecute(ConvertParameter(parameter));
    }

    public void Execute(object? parameter) => _execute(ConvertParameter(parameter));

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

    private static T? ConvertParameter(object? parameter)
    {
        if (parameter is null) return default;
        if (parameter is T typed) return typed;

        // Если из XAML пришла строка, а нам нужно число — конвертируем.
        try
        {
            return (T)Convert.ChangeType(parameter, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
