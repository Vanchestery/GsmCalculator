using System.IO;
using System.Text.Json;
using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class SessionService : ISessionService
{
    private readonly string _filePath;

    public SessionService(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public bool HasSavedSession => File.Exists(_filePath);

    public SessionState? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<SessionState>(json, JsonOptions.Default);
        }
        catch
        {
            return null;
        }
    }

    public void Save(SessionState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        state.SavedAt = DateTime.Now;

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, JsonOptions.Default);
        File.WriteAllText(_filePath, json);
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}
