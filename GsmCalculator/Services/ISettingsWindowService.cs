namespace GsmCalculator.Services;

/// <summary>
/// Открывает модальное окно настроек.
/// По аналогии с IWidgetWindowService изолирует MainViewModel
/// от знания о WPF-классе SettingsWindow.
/// </summary>
public interface ISettingsWindowService
{
    /// <summary>Открыть окно настроек как модальный диалог.</summary>
    void OpenDialog();
}
