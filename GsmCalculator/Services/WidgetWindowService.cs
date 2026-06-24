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
    private int _openedCounter;

    public WidgetWindowService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void OpenOrFocus(Widget widget)
    {
        if (widget is null) throw new ArgumentNullException(nameof(widget));

        // Уже открыт? Просто вытаскиваем поверх.
        if (_open.TryGetValue(widget.Id, out var existing))
        {
            if (!existing.IsVisible) existing.Show();
            existing.Activate();
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

        // Регистрируем окно как «сателлит» магнитной системы (v1.2 — блок J).
        // Отписка — при Closed (см. ниже).
        var magnetism = _sp.GetRequiredService<IWindowMagnetismService>();
        magnetism.RegisterSatellite(window);

        // При закрытии — убираем из реестра и освобождаем VM
        // (она отпишется от LanguageChanged, иначе утечка).
        window.Closed += (_, _) =>
        {
            magnetism.UnregisterSatellite(window);
            _open.Remove(widget.Id);
            vm.Dispose();
        };

        return (window, vm);
    }

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
