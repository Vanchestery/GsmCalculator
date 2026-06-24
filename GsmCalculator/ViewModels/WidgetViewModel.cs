using System.Globalization;
using System.Windows.Input;
using GsmCalculator.Models;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel для окна-виджета конвертации л↔кг.
///
/// Берёт значение с дисплея главного калькулятора, конвертирует и
/// показывает результат. По кнопке «Вставить в калькулятор»
/// пишет результат обратно на дисплей через MainViewModel.SetDisplayValue.
///
/// Подписан на смену языка: при переключении перерисовывает уже
/// показанный результат на новом языке. Поэтому реализует IDisposable —
/// отписка обязательна (LocalizationService — синглтон, иначе утечка).
/// </summary>
public class WidgetViewModel : ViewModelBase, IDisposable
{
    private readonly Widget _widget;
    private readonly IConversionService _conversion;
    private readonly ICalculatorService _calc;
    private readonly MainViewModel _mainVm;
    private readonly ILocalizationService _loc;
    private readonly IClipboardService _clipboard;
    private readonly IWidgetService _widgetService;
    private readonly IDebouncer _saveDebouncer;

    /// <summary>
    /// Подавление авто-сохранения. True во время инициализации полей
    /// в конструкторе и при <see cref="ApplyState"/> (восстановление сессии),
    /// чтобы не писать на диск то что только что с него прочитали.
    /// </summary>
    private bool _suspendAutoSave = true;

    /// <summary>Параметры последней конвертации — чтобы повторить её при смене языка.</summary>
    private sealed record LastConversion(double Input, double Density, int DecimalPlaces, bool IsLtoKg);
    private LastConversion? _last;

    public string Name => _widget.Name;

    /// <summary>True — плотность редактируемая. False — фиксированная (поле readonly).</summary>
    public bool IsDensityEditable => _widget.DensityMode == DensityMode.Variable;

    /// <summary>Инверсия IsDensityEditable — для биндинга на TextBox.IsReadOnly без конвертера.</summary>
    public bool IsDensityReadOnly => !IsDensityEditable;

    // --- Плотность (через строковый прокси чтобы парсить через InvariantCulture) ---
    private double _density;
    public double Density
    {
        get => _density;
        private set
        {
            if (SetProperty(ref _density, value))
            {
                OnPropertyChanged(nameof(DensityText));
                ScheduleSave();
            }
        }
    }

    /// <summary>
    /// Строковое представление плотности для биндинга в TextBox.
    /// Использует InvariantCulture (точка как разделитель) — консистентно
    /// с главным дисплеем калькулятора.
    /// </summary>
    public string DensityText
    {
        get => _density.ToString("G15", CultureInfo.InvariantCulture);
        set
        {
            // Принимаем любое распарсенное число (включая 0 и отрицательные).
            // Валидация «плотность > 0» живёт в ConversionService и кидает
            // исключение при конвертации — мы ловим и показываем в LastResult.
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                Density = parsed;
            // Если строка не парсится (буквы, пустая) — оставляем последнее валидное значение.
        }
    }

    // --- Округление 0..3 (биндится на Slider в WidgetWindow) ---
    private int _decimalPlaces;
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            if (SetProperty(ref _decimalPlaces, Math.Clamp(value, 0, 3)))
                ScheduleSave();
        }
    }

    // --- Результат последней конвертации ---
    private string _lastResult = string.Empty;
    public string LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    private double? _lastResultValue;
    public bool HasResult => _lastResultValue.HasValue;

    // --- Команды ---
    public ICommand LitersToKgCommand { get; }
    public ICommand KgToLitersCommand { get; }
    public ICommand InsertToCalculatorCommand { get; }
    public ICommand CopyResultCommand { get; }

    public WidgetViewModel(
        Widget widget,
        IConversionService conversion,
        ICalculatorService calc,
        MainViewModel mainVm,
        ILocalizationService localization,
        IClipboardService clipboard,
        IWidgetService widgetService,
        IDebouncer saveDebouncer)
    {
        _widget = widget ?? throw new ArgumentNullException(nameof(widget));
        _conversion = conversion ?? throw new ArgumentNullException(nameof(conversion));
        _calc = calc ?? throw new ArgumentNullException(nameof(calc));
        _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
        _loc = localization ?? throw new ArgumentNullException(nameof(localization));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _widgetService = widgetService ?? throw new ArgumentNullException(nameof(widgetService));
        _saveDebouncer = saveDebouncer ?? throw new ArgumentNullException(nameof(saveDebouncer));

        // _suspendAutoSave=true (поле-инициализатор) пока инициализируемся.
        _density = widget.DefaultDensity;
        _decimalPlaces = Math.Clamp(widget.DefaultDecimalPlaces, 0, 3);

        LitersToKgCommand = new RelayCommand(_ => Convert(isLtoKg: true));
        KgToLitersCommand = new RelayCommand(_ => Convert(isLtoKg: false));
        InsertToCalculatorCommand = new RelayCommand(_ => InsertToCalculator(), _ => HasResult);
        CopyResultCommand = new RelayCommand(_ => CopyResult(), _ => HasResult);

        // Перевод результата вживую при смене языка интерфейса.
        _loc.LanguageChanged += OnLanguageChanged;

        // Готовы реагировать на пользовательский ввод.
        _suspendAutoSave = false;
    }

    /// <summary>
    /// Планирует отложенное сохранение текущих Density/DecimalPlaces в виджет.
    /// Вызывается из сеттеров. Подавляется во время инициализации/ApplyState
    /// через <see cref="_suspendAutoSave"/>.
    /// </summary>
    private void ScheduleSave()
    {
        if (_suspendAutoSave) return;
        _saveDebouncer.Debounce(SaveCurrentState);
    }

    /// <summary>
    /// Сохраняет текущее состояние плотности/округления в <see cref="IWidgetService"/>.
    /// Re-fetch виджет по Id — на случай если за время дебаунса его отредактировали
    /// через Add Widget (E из v1.1). Так мы не затрём свежее Name/DensityMode.
    /// Если виджет был удалён — ничего не делаем (не воскрешаем).
    /// </summary>
    private void SaveCurrentState()
    {
        var current = _widgetService.Find(_widget.Id);
        if (current == null) return;

        var updated = new Widget
        {
            Id = current.Id,
            Name = current.Name,
            DensityMode = current.DensityMode,
            DefaultDensity = _density,
            DefaultDecimalPlaces = _decimalPlaces,
            IsBuiltIn = current.IsBuiltIn
        };

        try
        {
            _widgetService.Update(updated);
        }
        catch
        {
            // Сбой авто-сохранения не должен сломать виджет — следующая попытка может сработать.
            // (Например: пустое имя через рассинхрон с другой VM. WidgetService.Update бросает
            //  ArgumentException — но имя мы берём из current, так что не должно быть.)
        }
    }

    /// <summary>Запоминает параметры конвертации и выполняет её.</summary>
    private void Convert(bool isLtoKg)
    {
        var input = _mainVm.GetDisplayValue();
        // Помечаем что значение калькулятора было использовано — следующая
        // цифра должна начать новое число, а не дописать к существующему.
        // Иначе сценарий «100 → виджет → 200» давал бы «100200» на дисплее.
        _mainVm.NotifyDisplayConsumed();
        _last = new LastConversion(input, _density, _decimalPlaces, isLtoKg);
        ComputeAndShow();
    }

    /// <summary>
    /// Выполняет конвертацию из _last и обновляет LastResult.
    /// Вынесено отдельно, чтобы можно было повторить при смене языка.
    /// </summary>
    private void ComputeAndShow()
    {
        if (_last is not { } c) return;

        try
        {
            var raw = c.IsLtoKg
                ? _conversion.LitersToKilograms(c.Input, c.Density)
                : _conversion.KilogramsToLiters(c.Input, c.Density);

            var rounded = _conversion.Round(raw, c.DecimalPlaces);
            _lastResultValue = rounded;

            var key = c.IsLtoKg ? "Widget_ResultLtoKg" : "Widget_ResultKgToL";
            LastResult = _loc.GetFormat(key,
                _calc.FormatNumber(c.Input),
                _calc.FormatNumber(c.Density),
                _calc.FormatNumber(rounded));
        }
        catch (ArgumentException)
        {
            // Плотность <= 0 — конкретное локализованное сообщение.
            _lastResultValue = null;
            LastResult = _loc.Get("Widget_ErrorDensityPositive");
        }
        catch (Exception ex)
        {
            // Прочие ошибки — общий формат.
            _lastResultValue = null;
            LastResult = _loc.GetFormat("Common_ErrorFormat", ex.Message);
        }

        OnPropertyChanged(nameof(HasResult));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        // Перерисовываем уже показанный результат на новом языке.
        if (_last != null) ComputeAndShow();
    }

    private void InsertToCalculator()
    {
        if (_lastResultValue.HasValue)
            _mainVm.SetDisplayValue(_lastResultValue.Value);
    }

    /// <summary>
    /// Копирует в буфер обмена ТОЛЬКО число последнего результата
    /// (без единиц, как ответил пользователь в ТЗ 2.2). Уже округлённое
    /// значение из _lastResultValue, отформатированное через FormatNumber
    /// (тот же формат что и на дисплее калькулятора).
    /// </summary>
    private void CopyResult()
    {
        if (!_lastResultValue.HasValue) return;
        _clipboard.SetText(_calc.FormatNumber(_lastResultValue.Value));
    }

    /// <summary>
    /// Применяет сохранённое состояние при восстановлении сессии —
    /// плотность и округление, которые пользователь выставил до закрытия.
    /// Авто-сохранение временно подавлено: эти значения только что прочитаны
    /// с диска, писать их обратно нет смысла.
    /// </summary>
    public void ApplyState(double density, int decimalPlaces)
    {
        var previousSuspend = _suspendAutoSave;
        _suspendAutoSave = true;
        try
        {
            Density = density;
            DecimalPlaces = Math.Clamp(decimalPlaces, 0, 3);
        }
        finally
        {
            _suspendAutoSave = previousSuspend;
        }
    }

    /// <summary>
    /// Отписка от LanguageChanged + flush отложенного авто-сохранения.
    /// Вызывается при закрытии окна-виджета (см. WidgetWindowService).
    /// Без flush последнее изменение слайдера/плотности могло потеряться
    /// (если юзер закрыл окно до истечения debounce-задержки).
    /// </summary>
    public void Dispose()
    {
        _loc.LanguageChanged -= OnLanguageChanged;
        _saveDebouncer.Flush();
    }
}
