using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Управляет жизненным циклом окон-виджетов.
/// Хранит реестр открытых виджетов (один на Widget.Id),
/// открывает новые и активирует существующие.
/// </summary>
public interface IWidgetWindowService
{
    /// <summary>
    /// Открыть виджет или активировать уже открытое окно с тем же Id.
    /// </summary>
    void OpenOrFocus(Widget widget);

    /// <summary>
    /// Открыть виджет при восстановлении сессии — на сохранённой позиции,
    /// с сохранённой плотностью и округлением.
    /// </summary>
    void RestoreWidget(Widget widget, OpenWidgetState state);

    /// <summary>Снять состояние всех открытых виджетов для сохранения сессии.</summary>
    IReadOnlyList<OpenWidgetState> CaptureOpenWidgets();

    /// <summary>Закрыть конкретный виджет (если он открыт).</summary>
    void Close(Guid widgetId);

    /// <summary>Закрыть все открытые виджеты.</summary>
    void CloseAll();

    /// <summary>Id всех открытых на текущий момент виджетов.</summary>
    IReadOnlyCollection<Guid> GetOpenWidgetIds();

    /// <summary>
    /// Применяет режим «всегда поверх» к главному окну, открытым виджетам
    /// и окну «Добавить виджет» (если оно открыто).
    /// </summary>
    void ApplyAlwaysOnTop(bool alwaysOnTop);

    /// <summary>
    /// Прячет все видимые виджеты или возвращает тех, кого спрятал предыдущий вызов.
    /// Закрытые крестиком виджеты не воскресаются.
    /// </summary>
    void ToggleVisibility();

    /// <summary>
    /// После Restore из свёрнутого состояния Windows снова показывает owned-окна.
    /// Повторно прячем тех, кого спрятал тоггл.
    /// </summary>
    void ReapplyHiddenState();

    /// <summary>True, пока есть виджеты, спрятанные последним <see cref="ToggleVisibility"/>.</summary>
    bool AreWidgetsHidden { get; }

    /// <summary>Срабатывает при смене видимости пачки виджетов (тоггл, закрытие, повторное открытие).</summary>
    event EventHandler? VisibilityChanged;
}
