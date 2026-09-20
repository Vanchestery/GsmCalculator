using System.Windows;

namespace GsmCalculator.Helpers;

/// <summary>
/// Отсекает «мусорные» координаты окон: Win32 уносит свёрнутые окна
/// примерно в (-32000, -32000), и эти значения нельзя ни сохранять в сессию,
/// ни использовать как цель для магнитного следования виджетов.
/// </summary>
public static class WindowPositionHelper
{
    /// <summary>
    /// Ниже этого порога координата считается off-screen sentinel свёрнутого окна,
    /// а не реальной позицией на мониторе (даже при экзотическом VirtualScreen).
    /// </summary>
    public const double MinimizedSentinel = -10_000;

    /// <summary>
    /// Позиция годится для сохранения сессии и для следования сателлитов за хостом.
    /// </summary>
    public static bool IsRestorablePosition(double left, double top)
    {
        if (double.IsNaN(left) || double.IsNaN(top) ||
            double.IsInfinity(left) || double.IsInfinity(top))
            return false;

        if (left <= MinimizedSentinel || top <= MinimizedSentinel)
            return false;

        return ScreenHelper.IsOnScreen(left, top);
    }

    /// <summary>
    /// Свёрнутый хост (или уже унесённый в -32000) не должен тащить за собой виджеты.
    /// Иначе прилипшие окна переезжают off-screen, портят RestoreBounds
    /// и при следующем запуске раскладываются каскадом.
    /// </summary>
    public static bool ShouldSatellitesFollowHost(WindowState hostState, double left, double top)
        => hostState != WindowState.Minimized && IsRestorablePosition(left, top);
}
