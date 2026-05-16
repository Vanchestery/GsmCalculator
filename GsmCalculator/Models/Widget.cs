namespace GsmCalculator.Models;

/// <summary>
/// Определение виджета (POCO). Описывает то, что выбирается из списка
/// "Добавить виджет". Не содержит UI-состояния — для рантайма используется
/// <see cref="OpenWidgetState"/>.
///
/// Встроенные виджеты (АИ-92, ДТ-Л, ДТ-З, ТС-1, Масла, ТЖ, ОЖ) имеют
/// IsBuiltIn = true и не могут быть удалены пользователем.
/// </summary>
public class Widget
{
    /// <summary>
    /// Стабильный идентификатор. Используется в SessionState для привязки
    /// открытых окон-виджетов к их определениям.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Отображаемое имя виджета (например "АИ-92" или "Масла").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Фиксированная плотность или меняется пользователем.</summary>
    public DensityMode DensityMode { get; set; } = DensityMode.Variable;

    /// <summary>
    /// Для Fixed — это и есть плотность.
    /// Для Variable — начальное значение, которое можно изменить в виджете.
    /// </summary>
    public double DefaultDensity { get; set; }

    /// <summary>Дефолтное количество знаков после запятой (0..3).</summary>
    public int DefaultDecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Встроенный виджет (true) или созданный пользователем (false).
    /// Встроенные нельзя удалять.
    /// </summary>
    public bool IsBuiltIn { get; set; }
}
