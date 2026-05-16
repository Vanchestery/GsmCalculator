namespace GsmCalculator.Services;

/// <summary>
/// Открывает модальное окно создания пользовательского виджета.
/// </summary>
public interface ICreateWidgetWindowService
{
    /// <summary>
    /// Показать диалог создания виджета.
    /// Возвращает true, если пользователь создал виджет (нажал «Сохранить»).
    /// </summary>
    bool OpenDialog();
}
