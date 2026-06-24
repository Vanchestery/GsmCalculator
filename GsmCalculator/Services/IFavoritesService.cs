namespace GsmCalculator.Services;

/// <summary>
/// Управление списком «закреплённых» виджетов — Id'ы хранятся в
/// <see cref="Models.AppSettings.FavoriteWidgetIds"/>. Сервис инкапсулирует
/// чтение/запись и оповещает подписчиков об изменениях через <see cref="FavoritesChanged"/>.
///
/// Порядок Id в списке сохраняется — он же определяет порядок отображения
/// в панели «Избранное».
/// </summary>
public interface IFavoritesService
{
    /// <summary>Текущий список Id виджетов в избранном (порядок сохранён).</summary>
    IReadOnlyList<Guid> GetFavoriteIds();

    /// <summary>Закреплён ли виджет.</summary>
    bool IsFavorite(Guid widgetId);

    /// <summary>Закрепить (добавить в конец). Повторный вызов — no-op.</summary>
    void Add(Guid widgetId);

    /// <summary>Открепить. Если не был закреплён — no-op.</summary>
    void Remove(Guid widgetId);

    /// <summary>
    /// Если закреплён — открепить, иначе закрепить.
    /// Удобный «toggle» для UI-кнопки «★».
    /// </summary>
    void Toggle(Guid widgetId);

    /// <summary>
    /// Поднимается после Add/Remove/Toggle (если состояние реально изменилось).
    /// MainViewModel слушает чтобы перерисовать панель «Избранное».
    /// </summary>
    event EventHandler? FavoritesChanged;
}
