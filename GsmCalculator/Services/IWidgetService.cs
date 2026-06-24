using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Управление списком виджетов (встроенные + пользовательские).
/// Хранит данные в widgets.json. При первом запуске сеет встроенные виджеты.
/// </summary>
public interface IWidgetService
{
    /// <summary>Все доступные виджеты (встроенные + пользовательские).</summary>
    IReadOnlyList<Widget> GetAll();

    /// <summary>Найти виджет по Id (null если не существует).</summary>
    Widget? Find(Guid id);

    /// <summary>Добавить новый пользовательский виджет и сохранить файл.</summary>
    void Add(Widget widget);

    /// <summary>
    /// Обновить существующий виджет (по Id). Поля Name/DensityMode/DefaultDensity/
    /// DefaultDecimalPlaces перезаписываются; IsBuiltIn НЕ меняется — встроенный
    /// остаётся встроенным, пользовательский — пользовательским.
    /// Если виджет с таким Id не найден — no-op.
    /// </summary>
    void Update(Widget widget);

    /// <summary>
    /// Удалить пользовательский виджет. Бросает <see cref="InvalidOperationException"/>
    /// при попытке удалить встроенный.
    /// </summary>
    void Remove(Guid widgetId);

    /// <summary>Принудительно сохранить текущий список в файл.</summary>
    void SaveAll();

    /// <summary>
    /// Поднимается после любой модификации списка (Add/Update/Remove).
    /// Используется UI-слоем чтобы перерисовать зависимые представления —
    /// например, панель «Избранное» в главном окне или список виджетов
    /// в окне «Добавить виджет».
    /// </summary>
    event EventHandler? WidgetsChanged;
}
