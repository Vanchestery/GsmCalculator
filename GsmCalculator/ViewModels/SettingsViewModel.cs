using System.Windows.Input;
using GsmCalculator.Models;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel окна настроек. Держит редактируемые копии настроек.
/// По OK — строит новый AppSettings, сохраняет и применяет «на лету».
/// По Отмене — ничего не делает (мы ничего и не трогали до OK).
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly MainViewModel _mainVm;

    // Границы слайдера истории (по ТЗ 5..50).
    public int HistorySizeMin => 5;
    public int HistorySizeMax => 50;

    private int _historySize;
    public int HistorySize
    {
        get => _historySize;
        set => SetProperty(ref _historySize, value);
    }

    private ColorTheme _theme;
    public ColorTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    private StartupBehavior _startupBehavior;
    public StartupBehavior StartupBehavior
    {
        get => _startupBehavior;
        set => SetProperty(ref _startupBehavior, value);
    }

    private AppLanguage _language;
    public AppLanguage Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    private CalculatorMode _calculatorMode;
    public CalculatorMode CalculatorMode
    {
        get => _calculatorMode;
        set => SetProperty(ref _calculatorMode, value);
    }

    // Списки опций для ComboBox'ов.
    public IReadOnlyList<NamedOption<ColorTheme>> ThemeOptions { get; }
    public IReadOnlyList<NamedOption<StartupBehavior>> StartupOptions { get; }
    public IReadOnlyList<NamedOption<AppLanguage>> LanguageOptions { get; }
    public IReadOnlyList<NamedOption<CalculatorMode>> CalculatorModeOptions { get; }

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>Поднимается когда окно нужно закрыть. Аргумент: true — сохранено по OK.</summary>
    public event EventHandler<bool>? CloseRequested;

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        ILocalizationService localizationService,
        MainViewModel mainVm)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _localizationService = localizationService;
        _mainVm = mainVm;

        // Загружаем текущие настройки в редактируемые поля.
        var s = settingsService.Load();
        _historySize = Math.Clamp(s.HistorySize, HistorySizeMin, HistorySizeMax);
        _theme = s.Theme;
        _startupBehavior = s.StartupBehavior;
        _language = s.Language;
        _calculatorMode = s.CalculatorMode;

        // Подписи опций берём из языкового словаря. SettingsViewModel создаётся
        // заново при каждом открытии окна, поэтому подписи всегда в текущем языке.
        ThemeOptions = new[]
        {
            new NamedOption<ColorTheme>(ColorTheme.Light, localizationService.Get("Settings_ThemeLight")),
            new NamedOption<ColorTheme>(ColorTheme.Dark,  localizationService.Get("Settings_ThemeDark")),
            new NamedOption<ColorTheme>(ColorTheme.Blue,  localizationService.Get("Settings_ThemeBlue")),
        };
        StartupOptions = new[]
        {
            new NamedOption<StartupBehavior>(StartupBehavior.AlwaysAsk,      localizationService.Get("Settings_StartupAsk")),
            new NamedOption<StartupBehavior>(StartupBehavior.AlwaysContinue, localizationService.Get("Settings_StartupContinue")),
            new NamedOption<StartupBehavior>(StartupBehavior.AlwaysFresh,    localizationService.Get("Settings_StartupFresh")),
        };
        LanguageOptions = new[]
        {
            new NamedOption<AppLanguage>(AppLanguage.Russian, localizationService.Get("Settings_LangRu")),
            new NamedOption<AppLanguage>(AppLanguage.English, localizationService.Get("Settings_LangEn")),
        };
        CalculatorModeOptions = new[]
        {
            new NamedOption<CalculatorMode>(CalculatorMode.Classic,     localizationService.Get("Settings_ModeClassic")),
            new NamedOption<CalculatorMode>(CalculatorMode.Engineering, localizationService.Get("Settings_ModeEngineering")),
        };

        OkCommand = new RelayCommand(_ => Ok());
        CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));
    }

    private void Ok()
    {
        var updated = new AppSettings
        {
            HistorySize = HistorySize,
            Theme = Theme,
            StartupBehavior = StartupBehavior,
            Language = Language,
            CalculatorMode = CalculatorMode
        };

        // Сохраняем в settings.json.
        _settingsService.Save(updated);

        // Применяем «на лету» — без перезапуска приложения.
        _themeService.ApplyTheme(Theme);
        _localizationService.SetLanguage(Language);
        _mainVm.ApplyHistorySize(HistorySize);
        _mainVm.ApplyCalculatorMode(CalculatorMode);
        // StartupBehavior сохранён, но эффект только при следующем запуске.

        CloseRequested?.Invoke(this, true);
    }
}
