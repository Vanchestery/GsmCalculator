using System.Globalization;
using System.Windows.Input;
using GsmCalculator.Models;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel окна создания пользовательского виджета.
///
/// Поле плотности всегда обязательно: для Fixed это и есть плотность,
/// для Variable — начальное значение, которое потом можно менять в виджете.
/// (Widget.DefaultDensity должен быть валиден в любом случае.)
/// </summary>
public class CreateWidgetViewModel : ViewModelBase
{
    private readonly IWidgetService _widgetService;
    private readonly ILocalizationService _loc;
    private readonly RelayCommand _saveCommand;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set { if (SetProperty(ref _name, value)) Validate(); }
    }

    // По умолчанию — переменная плотность (чаще создают именно такие виджеты).
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

    // Округление 0..3 (биндится на Slider в CreateWidgetWindow). По умолчанию 0.
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

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand { get; }

    /// <summary>Поднимается когда окно нужно закрыть. true — виджет создан.</summary>
    public event EventHandler<bool>? CloseRequested;

    public CreateWidgetViewModel(IWidgetService widgetService, ILocalizationService localization)
    {
        _widgetService = widgetService;
        _loc = localization;

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
            Name = _name.Trim(),
            DensityMode = _isFixedDensity ? DensityMode.Fixed : DensityMode.Variable,
            DefaultDensity = density,
            DefaultDecimalPlaces = _decimalPlaces,
            IsBuiltIn = false
        };

        _widgetService.Add(widget);
        CloseRequested?.Invoke(this, true);
    }
}
