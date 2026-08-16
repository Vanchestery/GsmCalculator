using System.Windows;
using GsmCalculator.Helpers;
using GsmCalculator.Models;
using GsmCalculator.ViewModels;
using GsmCalculator.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class WidgetWindowService : IWidgetWindowService
{
    // IServiceProvider используем чтобы лениво резолвить MainViewModel —
    // прямой инжект MainViewModel сюда создал бы циклическую зависимость
    // (MainViewModel зависит от IWidgetWindowService).
    private readonly IServiceProvider _sp;

    private readonly Dictionary<Guid, WidgetWindow> _open = new();
    private readonly HashSet<Guid> _hiddenByToggle = new();
    private int _openedCounter;

    public bool AreWidgetsHidden => _hiddenByToggle.Count > 0;

    public event EventHandler? VisibilityChanged;

    public WidgetWindowService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void OpenOrFocus(Widget widget)
    {
        if (widget is null) throw new ArgumentNullException(nameof(widget));

        // Уже открыт? Просто вытаскиваем поверх.
        // Если этот виджет был спрятан тогглом — показываем только его,
        // остальные из пачки остаются скрытыми.
        if (_open.TryGetValue(widget.Id, out var existing))
        {
            var wasHidden = _hiddenByToggle.Remove(widget.Id);
            if (!existing.IsVisible) existing.Show();
            existing.Activate();
            if (wasHidden)
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var (window, _) = CreateWindow(widget);

        // Каскадное позиционирование справа от главного окна.
        PositionWindow(window);

        _open[widget.Id] = window;
        _openedCounter++;
        window.Show();
    }

    public void RestoreWidget(Widget widget, OpenWidgetState state)
    {
        if (widget is null) throw new ArgumentNullException(nameof(widget));
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (_open.ContainsKey(widget.Id)) return; // уже открыт

        var (window, vm) = CreateWindow(widget);
        vm.ApplyState(state.CurrentDensity, state.CurrentDecimalPlaces);

        // Восстанавливаем позицию, если она в пределах виртуального экрана.
        // Иначе (монитор отключили и т.п.) — раскладываем по умолчанию.
        if (ScreenHelper.IsOnScreen(state.Left, state.Top))
        {
            window.Left = state.Left;
            window.Top = state.Top;
        }
        else
        {
            PositionWindow(window);
        }

        _open[widget.Id] = window;
        _openedCounter++;
        window.Show();
    }

    public IReadOnlyList<OpenWidgetState> CaptureOpenWidgets()
    {
        var result = new List<OpenWidgetState>();

        foreach (var (id, window) in _open)
        {
            if (window.DataContext is not WidgetViewModel vm) continue;

            result.Add(new OpenWidgetState
            {
                WidgetId = id,
                Left = window.Left,
                Top = window.Top,
                CurrentDensity = vm.Density,
                CurrentDecimalPlaces = vm.DecimalPlaces
            });
        }

        return result;
    }

    public void Close(Guid widgetId)
    {
        if (_open.TryGetValue(widgetId, out var window))
            window.Close();
    }

    public void CloseAll()
    {
        // ToList — копия, иначе Closed-handler модифицирует словарь во время итерации.
        foreach (var w in _open.Values.ToList())
            w.Close();
    }

    public IReadOnlyCollection<Guid> GetOpenWidgetIds() => _open.Keys.ToList();

    public void ApplyAlwaysOnTop(bool alwaysOnTop)
    {
        var app = Application.Current;
        if (app?.MainWindow != null)
            app.MainWindow.Topmost = alwaysOnTop;

        foreach (var window in _open.Values)
            window.Topmost = alwaysOnTop;

        if (app == null) return;
        foreach (Window w in app.Windows)
        {
            if (w is AddWidgetWindow)
                w.Topmost = alwaysOnTop;
        }
    }

    public void ToggleVisibility()
    {
        if (AreWidgetsHidden)
            ShowHidden();
        else
            HideVisible();
    }

    public void ReapplyHiddenState()
    {
        foreach (var id in _hiddenByToggle)
        {
            if (_open.TryGetValue(id, out var window) && window.IsVisible)
                window.Hide();
        }
    }

    /// <summary>
    /// Создаёт ViewModel и окно виджета, вешает обработчик Closed.
    /// Общая часть для OpenOrFocus и RestoreWidget.
    /// </summary>
    private (WidgetWindow window, WidgetViewModel vm) CreateWindow(Widget widget)
    {
        var conversion = _sp.GetRequiredService<IConversionService>();
        var calc = _sp.GetRequiredService<ICalculatorService>();
        var mainVm = _sp.GetRequiredService<MainViewModel>();
        var loc = _sp.GetRequiredService<ILocalizationService>();
        var clipboard = _sp.GetRequiredService<IClipboardService>();
        var widgetService = _sp.GetRequiredService<IWidgetService>();
        var debouncer = _sp.GetRequiredService<IDebouncerFactory>()
            .Create(TimeSpan.FromMilliseconds(500));

        var vm = new WidgetViewModel(widget, conversion, calc, mainVm, loc, clipboard,
            widgetService, debouncer);
        var window = new WidgetWindow { DataContext = vm };

        // Owner привязывает виджет к главному окну: виджет остаётся над калькулятором,
        // но вместе с ним уходит под другие приложения (пока Topmost выключен).
        window.Owner = Application.Current?.MainWindow;
        window.Topmost = ReadAlwaysOnTop();

        // Регистрируем окно как «сателлит» магнитной системы (v1.2 — блок J).
        // Отписка — при Closed (см. ниже).
        var magnetism = _sp.GetRequiredService<IWindowMagnetismService>();
        magnetism.RegisterSatellite(window);
        // После Loaded известны высота (SizeToContent) и HWND для DWM-инсетов —
        // иначе восстановленная сессия не узнаёт уже прилипшие боковые виджеты.
        window.Loaded += (_, _) => magnetism.RefreshSnap(window);

        // При закрытии — убираем из реестра и освобождаем VM
        // (она отпишется от LanguageChanged, иначе утечка).
        window.Closed += (_, _) =>
        {
            magnetism.UnregisterSatellite(window);
            _open.Remove(widget.Id);
            var wasHidden = _hiddenByToggle.Remove(widget.Id);
            vm.Dispose();
            if (wasHidden)
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
        };

        return (window, vm);
    }

    /// <summary>Прячет все сейчас видимые виджеты. Состояние VM и позиция сохраняются.</summary>
    private void HideVisible()
    {
        foreach (var (id, window) in _open)
        {
            if (!window.IsVisible) continue;
            window.Hide();
            _hiddenByToggle.Add(id);
        }

        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Возвращает только тех, кого спрятал тоггл. Закрытые крестиком не трогаем.</summary>
    private void ShowHidden()
    {
        foreach (var id in _hiddenByToggle.ToList())
        {
            if (_open.TryGetValue(id, out var window))
                window.Show();
        }

        _hiddenByToggle.Clear();
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool ReadAlwaysOnTop()
        => _sp.GetRequiredService<ISettingsService>().Load().AlwaysOnTop;

    /// <summary>
    /// Раскладывает новые окна каскадом справа от главного окна.
    /// При выходе за пределы экрана возвращается влево.
    /// </summary>
    private void PositionWindow(Window window)
    {
        var main = Application.Current?.MainWindow;
        var offset = (_openedCounter % 8) * 30;

        if (main is null)
        {
            window.Left = 100 + offset;
            window.Top = 100 + offset;
            return;
        }

        window.Left = main.Left + main.ActualWidth + 10 + offset;
        window.Top = main.Top + 60 + offset;

        // Если уехали за правый край — кладём слева от главного.
        var screenW = SystemParameters.WorkArea.Width;
        if (window.Left + 300 > screenW)
            window.Left = Math.Max(0, main.Left - 310);
    }
}
