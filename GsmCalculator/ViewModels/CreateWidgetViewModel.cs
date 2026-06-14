using System.Globalization;
using System.Windows.Input;
using GsmCalculator.Models;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel окна создания/редактирования виджета.
///
/// Создаётся в двух режимах через одно и то же окно — переиспользование
/// валидации и UI:
/// - toEdit = null → create mode: новый виджет, IsBuiltIn=false, Add().
/// - toEdit != null → edit mode: префилл полей, Save() → Update() (сохраняя
///   исходный Id и IsBuiltIn).
///
/// Поле плотности обязательно в обоих режимах: для Fixed это и есть плотность,
/// для Variable — начальное значение, которое потом можно менять в виджете.
/// </summary>
public class CreateWidgetViewModel : ViewModelBase
{
    private readonly IWidgetService _widgetService;
    private readonly ILocalizationService _loc;
    private readonly RelayCommand _saveCommand;

    /// <summary>Id редактируемого виджета. null для режима «создание».</summary>
    private readonly Guid? _editingWidgetId;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set { if (SetProperty(ref _name, value)) Validate(); }
    }

    // По умолчанию (для create-mode) — переменная плотность.
    private bool _isFixedDensity = false;
    /// <summary>True — фиксированная плотность, False — переменная.</summary>
    public bool IsFixedDensity
    {
        get => _isFixedDensity;
        set
        {
            if (SetProperty(ref _isFixedDensity, value))
            {
                OnPropertyChanged(nameof(IsVariableDensity));
                Validate();
            }
        }
    }

    /// <summary>Зеркало IsFixedDensity для второй радиокнопки.</summary>
    public bool IsVariableDensity
    {
        get => !_isFixedDensity;
        set => IsFixedDensity = !value;
    }

    private string _densityText = "0.85";
    /// <summary>Плотность по умолчанию. Парсится через InvariantCulture (точка).</summary>
    public string DensityText
    {
        get => _densityText;
        set { if (SetProperty(ref _densityText, value)) Validate(); }
    }

    // Округление 0..3 (биндится на Slider в CreateWidgetWindow).
    private int _decimalPlaces = 0;
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set => SetProperty(ref _decimalPlaces, Math.Clamp(value, 0, 3));
    }

    private string _validationMessage = string.Empty;
    /// <summary>Текст ошибки валидации (пусто — всё ок).</summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    /// Локализованный заголовок окна. Зависит только от режима (set once
    /// в конструкторе) — диалог модальный, язык поменять во время него нельзя.
    /// </summary>
    public string WindowTitle
        => _loc.Get(_editingWidgetId.HasValue ? "CreateWidget_TitleEdit" : "CreateWidget_Title");

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand { get; }

    /// <summary>Поднимается когда окно нужно закрыть. true — виджет сохранён.</summary>
    public event EventHandler<bool>? CloseRequested;

    /// <param name="toEdit">
    /// Если задан — режим редактирования: префилл полей из widget, при Save
    /// вызывается Update вместо Add. Если null — режим создания.
    /// </param>
    public CreateWidgetViewModel(
        IWidgetService widgetService,
        ILocalizationService localization,
        Widget? toEdit = null)
    {
        _widgetService = widgetService;
        _loc = localization;

        if (toEdit != null)
        {
            // Edit mode — префилл из переданного widget'а.
            // Пишем в backing-поля напрямую: на этой стадии binding ещё не настроен
            // (DataContext назначается после конструктора), а Validate() в конце
            // конструктора всё равно пройдёт по этим значениям.
            _editingWidgetId = toEdit.Id;
            _name = toEdit.Name;
            _isFixedDensity = toEdit.DensityMode == DensityMode.Fixed;
            _densityText = toEdit.DefaultDensity.ToString(CultureInfo.InvariantCulture);
            _decimalPlaces = Math.Clamp(toEdit.DefaultDecimalPlaces, 0, 3);
        }

        _saveCommand = new RelayCommand(_ => Save(), _ => IsValid());
        CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

        Validate();
    }

    /// <summary>Парсит плотность; out-параметр — значение при успехе.</summary>
    private bool TryParseDensity(out double density)
        => double.TryParse(_densityText, NumberStyles.Float, CultureInfo.InvariantCulture, out density);

    private bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(_name)) return false;
        if (!TryParseDensity(out var d) || d <= 0) return false;
        return true;
    }

    /// <summary>Пересчитывает текст ошибки и доступность кнопки «Сохранить».</summary>
    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(_name))
            ValidationMessage = _loc.Get("CreateWidget_ValidationName");
        else if (!TryParseDensity(out var d))
            ValidationMessage = _loc.Get("CreateWidget_ValidationDensityNumber");
        else if (d <= 0)
            ValidationMessage = _loc.Get("CreateWidget_ValidationDensityPositive");
        else
            ValidationMessage = string.Empty;

        _saveCommand.RaiseCanExecuteChanged();
    }

    private void Save()
    {
        if (!IsValid()) return;
        TryParseDensity(out var density);

        var widget = new Widget
        {
            // В edit-режиме сохраняем исходный Id, в create — новый.
            Id = _editingWidgetId ?? Guid.NewGuid(),
            Name = _name.Trim(),
            DensityMode = _isFixedDensity ? DensityMode.Fixed : DensityMode.Variable,
            DefaultDensity = density,
            DefaultDecimalPlaces = _decimalPlaces,
            // В Update() этот флаг игнорируется (см. WidgetService.Update),
            // в Add() — пишется как есть. Для нового виджета всегда false.
            IsBuiltIn = false
        };

        if (_editingWidgetId.HasValue)
            _widgetService.Update(widget);
        else
            _widgetService.Add(widget);

        CloseRequested?.Invoke(this, true);
    }
}
