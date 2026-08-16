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
    /// <param name="satellite">Прямоугольник виджета (логические Left/Top/Width/Height).</param>
    /// <param name="host">Прямоугольник главного окна.</param>
    /// <param name="threshold">Максимальное расстояние до грани в пикселях.</param>
    /// <param name="hostInsets">DWM-тень хоста. Дистанция считается по визуальным граням,
    /// иначе после ApplySnap логический зазор (~14px) больше порога и снэп «отваливается»
    /// при восстановлении сессии.</param>
    /// <param name="satelliteInsets">DWM-тень сателлита.</param>
    public static SatelliteSnapState? TryFindSnap(
        Rect satellite, Rect host, double threshold,
        Thickness hostInsets = default, Thickness satelliteInsets = default)
    {
        var sat = ToVisual(satellite, satelliteInsets);
        var h = ToVisual(host, hostInsets);

        SatelliteSnapState? best = null;
        double bestDistance = double.MaxValue;

        // Offset оставляем логическим: ComputePosition ставит sat.Top = host.Top + offset.
        var logicalTopOffset = satellite.Top - host.Top;
        var logicalLeftOffset = satellite.Left - host.Left;

        // === Right: левая визуальная грань сателлита близко к правой визуальной грани хоста ===
        if (HasVerticalOverlap(sat, h))
        {
            var dist = Math.Abs(sat.Left - h.Right);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Right, logicalTopOffset);
            }
        }

        // === Left: правая визуальная грань сателлита близко к левой визуальной грани хоста ===
        if (HasVerticalOverlap(sat, h))
        {
            var dist = Math.Abs(sat.Right - h.Left);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Left, logicalTopOffset);
            }
        }

        // === Bottom: верхняя визуальная грань сателлита близко к нижней визуальной грани хоста ===
        if (HasHorizontalOverlap(sat, h))
        {
            var dist = Math.Abs(sat.Top - h.Bottom);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Bottom, logicalLeftOffset);
            }
        }

        // === Top: нижняя визуальная грань сателлита близко к верхней визуальной грани хоста ===
        if (HasHorizontalOverlap(sat, h))
        {
            var dist = Math.Abs(sat.Bottom - h.Top);
            if (dist <= threshold && dist < bestDistance)
            {
                bestDistance = dist;
                best = new SatelliteSnapState(SnapEdge.Top, logicalLeftOffset);
            }
        }

        return best;
    }

    /// <summary>Логический прямоугольник окна минус невидимая DWM-тень.</summary>
    private static Rect ToVisual(Rect r, Thickness insets)
        => new(
            r.Left + insets.Left,
            r.Top + insets.Top,
            Math.Max(0, r.Width - insets.Left - insets.Right),
            Math.Max(0, r.Height - insets.Top - insets.Bottom));

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
