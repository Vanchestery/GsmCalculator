using System.Windows;
using GsmCalculator.Helpers;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class WindowMagnetismService : IWindowMagnetismService
{
    /// <summary>Расстояние в пикселях, в пределах которого виджет «магнитится» к грани.</summary>
    private const double SnapThreshold = 12;

    private Window? _host;
    private readonly Dictionary<Window, SatelliteSnapState?> _satellites = new();

    /// <summary>
    /// True пока мы программно меняем Left/Top окна — чтобы не зациклиться:
    /// программное изменение Left/Top вызывает LocationChanged, который наш же
    /// обработчик начал бы перевычислять снэп заново.
    /// </summary>
    private bool _isPositioningProgrammatically;

    public void RegisterHost(Window host)
    {
        if (_host == host) return;
        if (_host != null)
        {
            _host.LocationChanged -= OnHostMoved;
            _host.StateChanged -= OnHostStateChanged;
        }

        _host = host;
        if (_host != null)
        {
            _host.LocationChanged += OnHostMoved;
            _host.StateChanged += OnHostStateChanged;
        }
    }

    public void RegisterSatellite(Window satellite)
    {
        if (_satellites.ContainsKey(satellite)) return;
        _satellites[satellite] = null; // изначально не прилеплен
        satellite.LocationChanged += OnSatelliteMoved;
    }

    public void UnregisterSatellite(Window satellite)
    {
        if (!_satellites.Remove(satellite)) return;
        satellite.LocationChanged -= OnSatelliteMoved;
    }

    public void RefreshSnap(Window satellite)
    {
        if (_host == null) return;
        if (!_satellites.ContainsKey(satellite)) return;

        _satellites[satellite] = TrySnap(satellite);
    }

    /// <summary>
    /// Сателлит подвинулся (либо юзер тянет, либо мы сами программно).
    /// Если двинули мы сами — пропускаем (флаг). Иначе — пересчитываем снэп.
    /// </summary>
    private void OnSatelliteMoved(object? sender, EventArgs e)
    {
        if (_isPositioningProgrammatically) return;
        if (_host == null) return;
        if (sender is not Window sat) return;
        if (!_satellites.ContainsKey(sat)) return;

        // Свёрнутое/унесённое Win32-окно: не пересчитываем снэп,
        // иначе прилипание «отваливается» и в сессию уедут -32000.
        if (!WindowPositionHelper.IsRestorablePosition(sat.Left, sat.Top))
            return;

        var snap = TrySnap(sat);
        _satellites[sat] = snap;

        if (snap != null)
            ApplySnap(sat, snap);
    }

    /// <summary>
    /// Хост подвинулся — двигаем все прилипшие сателлиты, сохраняя их сдвиги.
    /// Свёрнутый хост Win32 уносит в (-32000,-32000); за ним следовать нельзя.
    /// </summary>
    private void OnHostMoved(object? sender, EventArgs e)
    {
        if (_host == null) return;
        if (!WindowPositionHelper.ShouldSatellitesFollowHost(_host.WindowState, _host.Left, _host.Top))
            return;

        ReapplySnaps();
    }

    /// <summary>
    /// После Restore из свёрнутого состояния хост снова на экране —
    /// возвращаем прилипшие виджеты к его граням.
    /// </summary>
    private void OnHostStateChanged(object? sender, EventArgs e)
    {
        if (_host == null) return;
        if (!WindowPositionHelper.ShouldSatellitesFollowHost(_host.WindowState, _host.Left, _host.Top))
            return;

        ReapplySnaps();
    }

    private void ReapplySnaps()
    {
        foreach (var (sat, state) in _satellites)
        {
            if (state == null) continue;
            ApplySnap(sat, state);
        }
    }

    /// <summary>
    /// Программно ставит сателлит в положение, продиктованное snap-состоянием.
    /// Учитывает невидимые «тени» DWM, чтобы визуальные грани прилипали
    /// без зазора.
    /// </summary>
    private void ApplySnap(Window sat, SatelliteSnapState state)
    {
        if (_host == null) return;

        var hostInsets = WindowChromeHelper.GetVisualInsets(_host);
        var satInsets = WindowChromeHelper.GetVisualInsets(sat);

        var (left, top) = MagnetismCalculator.ComputePosition(
            WindowRect(_host), state, sat.Width, sat.Height,
            hostInsets, satInsets);

        if (!WindowPositionHelper.IsRestorablePosition(left, top))
            return;

        _isPositioningProgrammatically = true;
        try
        {
            sat.Left = left;
            sat.Top = top;
        }
        finally
        {
            _isPositioningProgrammatically = false;
        }
    }

    private SatelliteSnapState? TrySnap(Window sat)
    {
        if (_host == null) return null;
        return MagnetismCalculator.TryFindSnap(
            WindowRect(sat), WindowRect(_host), SnapThreshold,
            WindowChromeHelper.GetVisualInsets(_host),
            WindowChromeHelper.GetVisualInsets(sat));
    }

    private static Rect WindowRect(Window w)
        => new(w.Left, w.Top, w.Width, w.Height);
}
