namespace GsmCalculator.Models;

/// <summary>
/// Сериализуемое состояние сессии (session.json).
/// При закрытии приложения это сохраняется, при запуске — может
/// быть восстановлено (зависит от StartupBehavior и ответа пользователя).
/// </summary>
public class SessionState
{
    /// <summary>Что было на дисплее калькулятора в момент закрытия.</summary>
    public string CurrentDisplay { get; set; } = "0";

    /// <summary>История вычислений (новые в конце).</summary>
    public List<HistoryEntry> History { get; set; } = new();

    /// <summary>Список открытых виджетов на момент закрытия.</summary>
    public List<OpenWidgetState> OpenWidgets { get; set; } = new();

    /// <summary>Когда сессия была сохранена (для диагностики/UI).</summary>
    public DateTime SavedAt { get; set; } = DateTime.Now;

    public static SessionState CreateEmpty() => new();
}
