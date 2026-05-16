namespace GsmCalculator.Models;

/// <summary>
/// Настройки приложения — сериализуются в settings.json.
/// Это POCO без INotifyPropertyChanged: для UI настроек используется
/// SettingsViewModel, который оборачивает этот класс.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Сколько последних записей истории показывать в главном окне.
    /// Допустимый диапазон валидируется в SettingsViewModel (5..50 по ТЗ).
    /// </summary>
    public int HistorySize { get; set; } = 10;

    /// <summary>Текущая цветовая тема. По умолчанию тёмная — выглядит презентабельнее.</summary>
    public ColorTheme Theme { get; set; } = ColorTheme.Dark;

    /// <summary>Что делать при запуске приложения.</summary>
    public StartupBehavior StartupBehavior { get; set; } = StartupBehavior.AlwaysAsk;

    /// <summary>Язык интерфейса. По умолчанию русский (по ТЗ).</summary>
    public AppLanguage Language { get; set; } = AppLanguage.Russian;

    /// <summary>
    /// Режим калькулятора: классический (слева-направо) или инженерный
    /// (с приоритетом × и ÷ над + и −). По умолчанию классический.
    /// </summary>
    public CalculatorMode CalculatorMode { get; set; } = CalculatorMode.Classic;

    /// <summary>
    /// Возвращает копию настроек по умолчанию.
    /// Используется при первом запуске или сбросе настроек.
    /// </summary>
    public static AppSettings CreateDefault() => new();
}
