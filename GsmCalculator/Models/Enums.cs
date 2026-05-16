namespace GsmCalculator.Models;

/// <summary>
/// Тип плотности виджета.
/// </summary>
public enum DensityMode
{
    /// <summary>Плотность фиксированная и не редактируется в виджете (например, Масла = 0.9).</summary>
    Fixed,

    /// <summary>Плотность переменная — пользователь может изменить её в виджете (например, АИ-92).</summary>
    Variable
}

/// <summary>
/// Поведение приложения при запуске (настройка в окне Настроек).
/// </summary>
public enum StartupBehavior
{
    /// <summary>Спрашивать диалогом "Продолжить прошлую сессию?".</summary>
    AlwaysAsk,

    /// <summary>Всегда восстанавливать прошлую сессию без вопросов.</summary>
    AlwaysContinue,

    /// <summary>Всегда стартовать с чистого листа.</summary>
    AlwaysFresh
}

/// <summary>
/// Цветовая тема калькулятора.
/// Имена темы соответствуют файлам в Resources/Themes (LightTheme.xaml и т.д.).
/// </summary>
public enum ColorTheme
{
    Light,
    Dark,
    Blue
}

/// <summary>
/// Поддерживаемые языки интерфейса.
/// </summary>
public enum AppLanguage
{
    Russian,
    English
}

/// <summary>
/// Режим калькулятора:
/// - Classic: операции применяются слева направо (как Windows Calc «Стандартный»).
///   Пример: 5 − 1 × 3 = 12 (сначала 5−1=4, потом 4×3=12).
/// - Engineering: соблюдается приоритет × ÷ выше + −.
///   Пример: 5 − 1 × 3 = 2.
/// </summary>
public enum CalculatorMode
{
    Classic,
    Engineering
}
