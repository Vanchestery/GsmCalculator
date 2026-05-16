using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Загрузка/сохранение сессии (последний дисплей, открытые виджеты, история).
/// </summary>
public interface ISessionService
{
    /// <summary>Есть ли сохранённая сессия (файл существует и парсится).</summary>
    bool HasSavedSession { get; }

    /// <summary>Загрузить сессию. Возвращает null если сессии нет/повреждена.</summary>
    SessionState? Load();

    /// <summary>Сохранить сессию.</summary>
    void Save(SessionState state);

    /// <summary>Удалить сохранённую сессию.</summary>
    void Clear();
}
