using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Открывает модальное окно создания/редактирования виджета.
/// </summary>
public interface ICreateWidgetWindowService
{
    /// <summary>
    /// Показать диалог создания/редактирования виджета.
    /// </summary>
    /// <param name="toEdit">
    /// null — режим создания (новый виджет).
    /// Не-null — режим редактирования (префилл из переданного widget'а).
    /// </param>
    /// <returns>true если пользователь нажал «Сохранить».</returns>
    bool OpenDialog(Widget? toEdit = null);
}
