using System.Windows.Threading;

namespace GsmCalculator.Services;

/// <summary>
/// WPF-реализация <see cref="IDebouncer"/> на базе <see cref="DispatcherTimer"/>.
/// Tick срабатывает на UI-потоке — то что нужно для VM-кода
/// (без свободы потоков всё работает через PropertyChanged как обычно).
/// </summary>
public class DispatcherDebouncer : IDebouncer
{
    private readonly DispatcherTimer _timer;
    private Action? _pending;

    public DispatcherDebouncer(TimeSpan delay)
    {
        _timer = new DispatcherTimer { Interval = delay };
        _timer.Tick += OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        var p = _pending;
        _pending = null;
        p?.Invoke();
    }

    public void Debounce(Action callback)
    {
        _pending = callback;
        _timer.Stop();
        _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
        _pending = null;
    }

    public void Flush()
    {
        if (_pending == null) return;
        _timer.Stop();
        var p = _pending;
        _pending = null;
        p.Invoke();
    }
}

/// <inheritdoc/>
public class DispatcherDebouncerFactory : IDebouncerFactory
{
    public IDebouncer Create(TimeSpan delay) => new DispatcherDebouncer(delay);
}
