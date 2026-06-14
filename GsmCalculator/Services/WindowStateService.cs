using System.IO;
using System.Text.Json;
using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class WindowStateService : IWindowStateService
{
    private readonly string _filePath;

    public WindowStateService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public MainWindowState? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<MainWindowState>(json, JsonOptions.Default);
        }
        catch
        {
            // Файл повреждён — возвращаем null, окно откроется по дефолту.
            return null;
        }
    }

    public void Save(MainWindowState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, JsonOptions.Default);
        File.WriteAllText(_filePath, json);
    }
}
