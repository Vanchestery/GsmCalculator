using System.IO;
using System.Text.Json;
using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class SettingsService : ISettingsService
{
    private readonly string _filePath;

    /// <param name="filePath">Полный путь к settings.json. Конкретное значение
    /// передаётся при регистрации DI (см. App.xaml.cs), что упрощает тестирование.</param>
    public SettingsService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return AppSettings.CreateDefault();

        try
        {
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions.Default);
            return loaded ?? AppSettings.CreateDefault();
        }
        catch
        {
            // Файл повреждён — не падаем, возвращаем дефолты.
            // (В продакшене сюда стоит добавить логирование.)
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions.Default);

        // Гарантируем что папка существует — на случай первого запуска.
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(_filePath, json);
    }
}
