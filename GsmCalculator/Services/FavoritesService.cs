namespace GsmCalculator.Services;

/// <inheritdoc/>
public class FavoritesService : IFavoritesService
{
    private readonly ISettingsService _settings;
    private readonly List<Guid> _ids;

    public FavoritesService(ISettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Берём снапшот из settings.json при создании сервиса (singleton, живёт всю сессию).
        var s = settings.Load();
        _ids = new List<Guid>(s.FavoriteWidgetIds ?? new List<Guid>());
    }

    public IReadOnlyList<Guid> GetFavoriteIds() => _ids.AsReadOnly();

    public bool IsFavorite(Guid widgetId) => _ids.Contains(widgetId);

    public event EventHandler? FavoritesChanged;

    public void Add(Guid widgetId)
    {
        if (_ids.Contains(widgetId)) return;
        _ids.Add(widgetId);
        Persist();
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Guid widgetId)
    {
        if (!_ids.Remove(widgetId)) return;
        Persist();
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle(Guid widgetId)
    {
        if (_ids.Contains(widgetId))
            Remove(widgetId);
        else
            Add(widgetId);
    }

    /// <summary>
    /// Перезаписывает FavoriteWidgetIds в settings.json. Прочие настройки
    /// сохраняются (Load+Save pattern, как в MainViewModel.CycleRoundingMode).
    /// </summary>
    private void Persist()
    {
        var s = _settings.Load();
        s.FavoriteWidgetIds = new List<Guid>(_ids);
        _settings.Save(s);
    }
}
