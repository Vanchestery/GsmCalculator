namespace GsmCalculator.Models;

/// <summary>
/// Состояние одного открытого окна-виджета на экране (POCO).
/// Используется для сохранения сессии: чтобы при перезапуске
/// восстановить положение, плотность и округление, которые
/// пользователь установил.
///
/// Сам виджет (его определение) ищется по WidgetId в WidgetService.
/// </summary>
public class OpenWidgetState
{
    /// <summary>Id виджета из списка определений (см. <see cref="Widget.Id"/>).</summary>
    public Guid WidgetId { get; set; }

    /// <summary>Координата левого края окна (Window.Left).</summary>
    public double Left { get; set; }

    /// <summary>Координата верхнего края окна (Window.Top).</summary>
    public double Top { get; set; }

    /// <summary>
    /// Текущая плотность, выставленная пользователем в виджете.
    /// Для Fixed-виджетов будет равна Widget.DefaultDensity.
    /// </summary>
    public double CurrentDensity { get; set; }

    /// <summary>Текущее выбранное округление (0..3).</summary>
    public int CurrentDecimalPlaces { get; set; } = 2;
}
