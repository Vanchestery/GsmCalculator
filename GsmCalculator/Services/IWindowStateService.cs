using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Загрузка/сохранение положения и размера главного окна в window-state.json.
/// Это отдельный от сессии файл — окно запоминается ВСЕГДА, независимо
/// от настройки StartupBehavior.
/// </summary>
public interface IWindowStateService
{
    /// <summary>Загружает сохранённое состояние. Возвращает null если файла нет/повреждён.</summary>
    MainWindowState? Load();

    /// <summary>Сохраняет состояние окна на диск.</summary>
    void Save(MainWindowState state);
}
