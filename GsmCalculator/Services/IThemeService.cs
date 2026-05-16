using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Управляет цветовой темой приложения. Подменяет тему-словарь
/// в Application.Current.Resources.MergedDictionaries — благодаря
/// {DynamicResource} в XAML смена темы происходит «на лету».
/// </summary>
public interface IThemeService
{
    ColorTheme CurrentTheme { get; }

    /// <summary>Применить тему (мгновенно перекрашивает все окна).</summary>
    void ApplyTheme(ColorTheme theme);
}
