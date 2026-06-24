using System.Windows;

namespace GsmCalculator.Helpers;

/// <summary>
/// К какой грани главного окна «прилеплен» виджет.
/// </summary>
public enum SnapEdge
{
    /// <summary>Левая грань сателлита прижата к правой грани хоста.</summary>
    Right,
    /// <summary>Правая грань сателлита прижата к левой грани хоста.</summary>
    Left,
    /// <summary>Верхняя грань сателлита прижата к нижней грани хоста.</summary>
    Bottom,
    /// <summary>Нижняя грань сателлита прижата к верхней грани хоста.</summary>
    Top
}

/// <summary>
/// Состояние «прилипшего» виджета: к какой грани и со сдвигом
/// вдоль этой грани относительно угла хоста.
///
/// Для Right/Left: <see cref="Offset"/> — это сдвиг по Y от Top хоста.
/// Для Top/Bottom: <see cref="Offset"/> — это сдвиг по X от Left хоста.
/// </summary>
public sealed record SatelliteSnapState(SnapEdge Edge, double Offset);

/// <summary>
/// Чистая логика магнитного прилипания виджетов к главному окну.
/// Без зависимостей от WPF — принимает <see cref="Rect"/>, возвращает данные.
/// Юнит-тестируется отдельно от UI.
/// </summary>
public static class MagnetismCalculator
{
    /// <summary>
    /// Пытается найти ближайшую грань хоста, к которой стоит «прилипить» сателлита.
    /// Возвращает null если никакая грань не подходит (слишком далеко или нет перекрытия).
    ///
    /// Перекрытие требуется: например, «правую» грань нельзя зацепить если
    /// сателлит висит на 100 пикселей выше или ниже хоста — он будет рядом,
    /// но магнитно неинтуитивно.
    /// </summary>
    /// <param name="satellite">Прямоугольник виджета.</param>
    /// <param name="host">Прямоугольник главного окна.</param>
    /// <param name="threshold">Максимальное расстояние до грани в пикселях.</param>
    public static SatelliteSnapState? TryFindSnap(Rect satellite, Rect host, double threshold)
    {
        SatelliteSnapState? best = null;
        double bestDistance = double.MaxValue;

        // === Right: левая грань сателлита близко к правой грани хоста ===
        // Требует вертикального перекрытия с хостом.
        if (HasVerticalOverlap(satellite, host))
        {
            var dist = Math.Abs(satellite.Left - host.Right);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Right, satellite.Top - host.Top);
            }
        }

        // === Left: правая грань сателлита близко к левой грани хоста ===
        if (HasVerticalOverlap(satellite, host))
        {
            var dist = Math.Abs((satellite.Left + satellite.Width) - host.Left);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Left, satellite.Top - host.Top);
            }
        }

        // === Bottom: верхняя грань сателлита близко к нижней грани хоста ===
        if (HasHorizontalOverlap(satellite, host))
        {
            var dist = Math.Abs(satellite.Top - host.Bottom);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Bottom, satellite.Left - host.Left);
            }
        }

        // === Top: нижняя грань сателлита близко к верхней грани хоста ===
        if (HasHorizontalOverlap(satellite, host))
        {
            var dist = Math.Abs((satellite.Top + satellite.Height) - host.Top);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Top, satellite.Left - host.Left);
            }
        }

        return best;
    }

    /// <summary>
    /// Возвращает Left/Top сателлита для того чтобы он сидел в указанном snap-состоянии
    /// относительно текущего положения хоста.
    ///
    /// hostInsets/satelliteInsets — невидимые «тени» DWM. Без них была бы видимая
    /// дырка между окнами в ~14px (см. <see cref="WindowChromeHelper.GetVisualInsets"/>).
    /// По умолчанию нули — старое поведение для unit-тестов чистой геометрии.
    /// </summary>
    public static (double Left, double Top) ComputePosition(
        Rect host, SatelliteSnapState state, double satelliteWidth, double satelliteHeight,
        Thickness hostInsets = default, Thickness satelliteInsets = default)
    {
        return state.Edge switch
        {
            // Визуально: sat.VisualLeft == host.VisualRight
            // → sat.Left + satInsets.Left = host.Right - hostInsets.Right
            // → sat.Left = host.Right - hostInsets.Right - satInsets.Left
            SnapEdge.Right => (
                host.Right - hostInsets.Right - satelliteInsets.Left,
                host.Top + state.Offset),

            // Визуально: sat.VisualRight == host.VisualLeft
            // → sat.Left + sat.Width - satInsets.Right = host.Left + hostInsets.Left
            // → sat.Left = host.Left + hostInsets.Left + satInsets.Right - sat.Width
            SnapEdge.Left  => (
                host.Left + hostInsets.Left + satelliteInsets.Right - satelliteWidth,
                host.Top + state.Offset),

            SnapEdge.Bottom => (
                host.Left + state.Offset,
                host.Bottom - hostInsets.Bottom - satelliteInsets.Top),

            SnapEdge.Top   => (
                host.Left + state.Offset,
                host.Top + hostInsets.Top + satelliteInsets.Bottom - satelliteHeight),

            _ => (host.Left, host.Top)
        };
    }

    /// <summary>Пересекаются ли прямоугольники по вертикали (для Right/Left snap).</summary>
    private static bool HasVerticalOverlap(Rect a, Rect b)
        => a.Top < b.Bottom && a.Bottom > b.Top;

    /// <summary>Пересекаются ли прямоугольники по горизонтали (для Top/Bottom snap).</summary>
    private static bool HasHorizontalOverlap(Rect a, Rect b)
        => a.Left < b.Right && a.Right > b.Left;
}
