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
            _host.LocationChanged -= OnHostMoved;

        _host = host;
        if (_host != null)
            _host.LocationChanged += OnHostMoved;
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

        var snap = MagnetismCalculator.TryFindSnap(WindowRect(sat), WindowRect(_host), SnapThreshold);
        _satellites[sat] = snap;

        if (snap != null)
            ApplySnap(sat, snap);
    }

    /// <summary>
    /// Хост подвинулся — двигаем все прилипшие сателлиты, сохраняя их сдвиги.
    /// </summary>
    private void OnHostMoved(object? sender, EventArgs e)
    {
        if (_host == null) return;

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

    private static Rect WindowRect(Window w)
        => new(w.Left, w.Top, w.Width, w.Height);
}
