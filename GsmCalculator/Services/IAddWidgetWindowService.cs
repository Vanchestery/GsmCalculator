namespace GsmCalculator.Services;

/// <summary>
/// Открывает немодальное окно «Добавить виджет» (список виджетов).
/// Окно в единственном экземпляре — повторный вызОв активирует существующее.
/// </summary>
public interface IAddWidgetWindowService
{
    void OpenDialog();
}
