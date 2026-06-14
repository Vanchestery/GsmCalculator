using System.Windows;

namespace GsmCalculator.Helpers;

/// <summary>
/// Утилиты для проверки положения окон относительно текущей конфигурации экранов.
/// </summary>
public static class ScreenHelper
{
    /// <summary>
    /// Проверяет что точка (left, top) попадает в видимую область виртуального
    /// экрана (объединение всех мониторов) с запасом, чтобы окно не оказалось
    /// почти за краем при восстановлении.
    ///
    /// Используется когда восстанавливаем сохранённую позицию окна — если
    /// пользователь отключил монитор, на котором было окно, fallback на дефолт.
    /// </summary>
    public static bool IsOnScreen(double left, double top, double margin = 100)
    {
        var vLeft   = SystemParameters.VirtualScreenLeft;
        var vTop    = SystemParameters.VirtualScreenTop;
        var vRight  = vLeft + SystemParameters.VirtualScreenWidth;
        var vBottom = vTop + SystemParameters.VirtualScreenHeight;

        return left >= vLeft && left <= vRight - margin
            && top  >= vTop  && top  <= vBottom - margin;
    }
}
