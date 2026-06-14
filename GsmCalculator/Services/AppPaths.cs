using System.IO;

namespace GsmCalculator.Services;

/// <summary>
/// Пути к файлам данных приложения в %AppData%\GsmCalculator.
/// Это статический класс — пути не зависят от состояния и не меняются
/// во время работы. Для тестируемости конкретные сервисы принимают
/// нужный им путь через конструктор (а не зовут этот класс напрямую).
/// Связывание делается в App.xaml.cs при регистрации DI.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// Папка %AppData%\GsmCalculator (создаётся при необходимости).
    /// На разных языках Windows это разный физический путь,
    /// поэтому используем Environment.SpecialFolder.
    /// </summary>
    public static string AppDataDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GsmCalculator");

            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFile    => Path.Combine(AppDataDir, "settings.json");
    public static string WidgetsFile     => Path.Combine(AppDataDir, "widgets.json");
    public static string SessionFile     => Path.Combine(AppDataDir, "session.json");
    public static string WindowStateFile => Path.Combine(AppDataDir, "window-state.json");
}
