using System.Windows;
using GsmCalculator.Helpers;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на MagnetismCalculator — чистая логика прилипания виджетов
/// к граням главного окна (блок J v1.2). WPF не требуется.
/// </summary>
public class MagnetismCalculatorTests
{
    // Хост стоит на (100, 100), размер 500x400 — Right=600, Bottom=500.
    private static readonly Rect Host = new(100, 100, 500, 400);
    private const double Threshold = 10;

    // ----------------------------------------------------------------
    // TryFindSnap — обнаружение grани и расстояния
    // ----------------------------------------------------------------

    [Fact]
    public void TryFindSnap_FarAway_ReturnsNull()
    {
        var sat = new Rect(800, 800, 200, 150);
        Assert.Null(MagnetismCalculator.TryFindSnap(sat, Host, Threshold));
    }

    [Fact]
    public void TryFindSnap_NearRightEdge_WithVerticalOverlap_SnapsRight()
    {
        // Сателлит сразу справа от хоста (с маленьким зазором 5px) — в пределах threshold=10.
        var sat = new Rect(605, 150, 200, 150);
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.NotNull(snap);
        Assert.Equal(SnapEdge.Right, snap!.Edge);
        // Offset = sat.Top - host.Top = 150 - 100 = 50.
        Assert.Equal(50, snap.Offset);
    }

    [Fact]
    public void TryFindSnap_NearLeftEdge_WithVerticalOverlap_SnapsLeft()
    {
        // Сателлит шириной 200, справа от него (сателлита) — левая грань хоста (X=100).
        // Чтобы правая грань сателлита была в районе X=100, его Left = -100.
        var sat = new Rect(-105, 150, 200, 150); // sat.Right = 95, до host.Left=100 — 5px.
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.NotNull(snap);
        Assert.Equal(SnapEdge.Left, snap!.Edge);
        Assert.Equal(50, snap.Offset);
    }

    [Fact]
    public void TryFindSnap_NearBottomEdge_WithHorizontalOverlap_SnapsBottom()
    {
        var sat = new Rect(200, 505, 200, 150); // top=505, host.Bottom=500 → 5px
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.NotNull(snap);
        Assert.Equal(SnapEdge.Bottom, snap!.Edge);
        // Offset = sat.Left - host.Left = 200 - 100 = 100.
        Assert.Equal(100, snap.Offset);
    }

    [Fact]
    public void TryFindSnap_NearTopEdge_WithHorizontalOverlap_SnapsTop()
    {
        // Сателлит высотой 150, его bottom близко к host.Top=100.
        // Bottom=100 → Top = -50. С зазором 5 → Top = -55, Bottom = 95.
        var sat = new Rect(200, -55, 200, 150);
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.NotNull(snap);
        Assert.Equal(SnapEdge.Top, snap!.Edge);
        Assert.Equal(100, snap.Offset);
    }

    [Fact]
    public void TryFindSnap_NearRightEdge_NoVerticalOverlap_DoesNotSnap()
    {
        // Сателлит справа, но выше хоста — не должен прилипнуть.
        var sat = new Rect(605, -200, 200, 150);
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.Null(snap);
    }

    [Fact]
    public void TryFindSnap_ExactlyAtThreshold_Snaps()
    {
        // Расстояние ровно на threshold — должно прилипнуть.
        var sat = new Rect(610, 150, 200, 150); // 600+10 = граница
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.NotNull(snap);
        Assert.Equal(SnapEdge.Right, snap!.Edge);
    }

    [Fact]
    public void TryFindSnap_JustBeyondThreshold_DoesNotSnap()
    {
        var sat = new Rect(611, 150, 200, 150); // 11px от грани > 10
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.Null(snap);
    }

    [Fact]
    public void TryFindSnap_TwoEdgesPossible_PicksCloser()
    {
        // Сателлит в правом-нижнем углу: близко и к right и к bottom хоста.
        // Right distance = sat.Left - host.Right.
        // Bottom distance = sat.Top - host.Bottom.
        // sat=(602, 508) → right_dist=2, bottom_dist=8 → пикаем Right.
        var sat = new Rect(602, 508, 200, 150);
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);

        Assert.NotNull(snap);
        Assert.Equal(SnapEdge.Right, snap!.Edge);
    }

    // ----------------------------------------------------------------
    // ComputePosition — куда поставить сателлит при заданном snap-состоянии
    // ----------------------------------------------------------------

    [Fact]
    public void ComputePosition_Right_PlacesAtHostRightEdge()
    {
        var state = new SatelliteSnapState(SnapEdge.Right, 50);
        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150);

        Assert.Equal(600, left); // host.Right
        Assert.Equal(150, top);  // host.Top + offset = 100 + 50
    }

    [Fact]
    public void ComputePosition_Left_PlacesAtHostLeftEdgeMinusSatelliteWidth()
    {
        var state = new SatelliteSnapState(SnapEdge.Left, 50);
        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150);

        Assert.Equal(-100, left); // host.Left - sat.Width = 100 - 200
        Assert.Equal(150, top);
    }

    [Fact]
    public void ComputePosition_Bottom_PlacesAtHostBottomEdge()
    {
        var state = new SatelliteSnapState(SnapEdge.Bottom, 100);
        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150);

        Assert.Equal(200, left); // host.Left + offset = 100 + 100
        Assert.Equal(500, top);  // host.Bottom
    }

    [Fact]
    public void ComputePosition_Top_PlacesAtHostTopEdgeMinusSatelliteHeight()
    {
        var state = new SatelliteSnapState(SnapEdge.Top, 100);
        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150);

        Assert.Equal(200, left);
        Assert.Equal(-50, top); // host.Top - sat.Height = 100 - 150
    }

    [Fact]
    public void Roundtrip_SnapThenCompute_LandsAtExactEdge()
    {
        // Sat около правой грани хоста с offset. После snap+compute
        // он должен оказаться ровно на грани с тем же offset.
        var sat = new Rect(606, 180, 200, 150);
        var snap = MagnetismCalculator.TryFindSnap(sat, Host, Threshold);
        Assert.NotNull(snap);

        var (left, top) = MagnetismCalculator.ComputePosition(Host, snap!, sat.Width, sat.Height);

        Assert.Equal(Host.Right, left);
        // offset был sat.Top - host.Top = 80; должно дать host.Top + 80 = 180.
        Assert.Equal(180, top);
    }

    // ----------------------------------------------------------------
    // ComputePosition с DWM-инсетами (фикс gap'а между визуальными гранями)
    // ----------------------------------------------------------------

    [Fact]
    public void ComputePosition_Right_WithInsets_RemovesShadowGap()
    {
        // Тень 7px с правой грани хоста и 7px с левой грани сателлита.
        // Без insets: sat.Left = host.Right = 600 → визуально 14px зазор.
        // С insets:   sat.Left = 600 - 7 - 7 = 586 → визуально 0 зазор.
        var hostInsets = new Thickness(left: 7, top: 7, right: 7, bottom: 7);
        var satInsets  = new Thickness(left: 7, top: 7, right: 7, bottom: 7);
        var state = new SatelliteSnapState(SnapEdge.Right, 50);

        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150, hostInsets, satInsets);

        Assert.Equal(586, left); // 600 - 7 - 7
        Assert.Equal(150, top);
    }

    [Fact]
    public void ComputePosition_Left_WithInsets_RemovesShadowGap()
    {
        var insets = new Thickness(7);
        var state = new SatelliteSnapState(SnapEdge.Left, 50);

        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150, insets, insets);

        // host.Left + 7 + 7 - 200 = 100 + 14 - 200 = -86
        Assert.Equal(-86, left);
        Assert.Equal(150, top);
    }

    [Fact]
    public void ComputePosition_Bottom_WithInsets_RemovesShadowGap()
    {
        var insets = new Thickness(7);
        var state = new SatelliteSnapState(SnapEdge.Bottom, 100);

        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150, insets, insets);

        Assert.Equal(200, left);
        Assert.Equal(486, top); // host.Bottom 500 - 7 - 7
    }

    [Fact]
    public void ComputePosition_Top_WithInsets_RemovesShadowGap()
    {
        var insets = new Thickness(7);
        var state = new SatelliteSnapState(SnapEdge.Top, 100);

        var (left, top) = MagnetismCalculator.ComputePosition(Host, state, 200, 150, insets, insets);

        Assert.Equal(200, left);
        Assert.Equal(-36, top); // host.Top 100 + 7 + 7 - 150
    }

    [Fact]
    public void ComputePosition_DefaultInsets_BehaviourUnchanged()
    {
        // Без insets-параметров — старое поведение (Thickness default = 0).
        var state = new SatelliteSnapState(SnapEdge.Right, 50);
        var (left, _) = MagnetismCalculator.ComputePosition(Host, state, 200, 150);
        Assert.Equal(600, left);
    }
}
