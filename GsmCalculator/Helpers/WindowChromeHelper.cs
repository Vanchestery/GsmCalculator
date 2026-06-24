using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GsmCalculator.Helpers;

/// <summary>
/// Запрашивает у DWM реальные ВИЗУАЛЬНЫЕ границы окна — без невидимой тени/resize-grip,
/// которые Windows 10/11 рисует за пределами визуального фрейма.
///
/// Используется блоком J (магнитное прилипание): <see cref="Window.Left"/> и
/// <see cref="Window.Width"/> указывают на ВНЕШНИЕ границы (включая тень), и если
/// просто выставить <c>sat.Left = host.Right</c>, визуально остаётся зазор
/// (~7px с каждой стороны = ~14px между виджетами).
/// </summary>
public static class WindowChromeHelper
{
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    /// <summary>
    /// Возвращает «инсеты» — на сколько визуальная грань окна отстоит ВНУТРЬ
    /// от <see cref="Window.Left"/> / Top / Right / Bottom. Положительные значения
    /// = ширина невидимой тени. Если DWM недоступен — все нули (поведение как раньше).
    /// </summary>
    public static Thickness GetVisualInsets(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return new Thickness(0);

        var sizeOfRect = Marshal.SizeOf<RECT>();
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out var frame, sizeOfRect) != 0)
            return new Thickness(0);

        // DWM возвращает device pixels — переводим в DIP'ы под текущий DPI монитора,
        // потому что Window.Left/Top/Width уже в DIP'ах.
        var source = HwndSource.FromHwnd(hwnd);
        if (source?.CompositionTarget == null)
            return new Thickness(0);

        var fromDevice = source.CompositionTarget.TransformFromDevice;
        var visualTopLeft = fromDevice.Transform(new Point(frame.Left, frame.Top));
        var visualBottomRight = fromDevice.Transform(new Point(frame.Right, frame.Bottom));

        // Положительные значения = тень есть.
        // window.Left + leftInset = visualLeft.
        return new Thickness(
            left:   visualTopLeft.X - window.Left,
            top:    visualTopLeft.Y - window.Top,
            right:  (window.Left + window.Width)  - visualBottomRight.X,
            bottom: (window.Top  + window.Height) - visualBottomRight.Y
        );
    }
}
