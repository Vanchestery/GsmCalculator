namespace GsmCalculator.Services;

/// <summary>
/// Тонкая обёртка над системным буфером обмена. Существует ради
/// возможности подменить реализацию в тестах (System.Windows.Clipboard
/// требует STA-поток и нестабилен в headless-окружении).
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Кладёт текст в буфер обмена. Безопасна — при сбое
    /// (буфер занят другим процессом, COM-ошибка) ничего не делает.
    /// </summary>
    void SetText(string text);
}
