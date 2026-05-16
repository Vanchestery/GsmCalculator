using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GsmCalculator.Services;

/// <summary>
/// Красит нативную полосу заголовка окна в тёмный/светлый режим
/// через DWM (Desktop Window Manager) API.
///
/// Работает на Windows 10 (билд 1809+) и Windows 11.
/// На более старых ОС вызовы вернут ошибку — мы её просто игнорируем,
/// заголовок останется системным (приложение не падает).
/// </summary>
public static class TitleBarHelper
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Атрибут «использовать тёмный режим для заголовка».
    // 20 — Windows 10 20H1+ и Windows 11. 19 — Windows 10 1809..1909.
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    /// <summary>
    /// Применить тёмный/светлый заголовок к окну.
    /// Окно должно быть уже создано (иметь HWND) — вызывайте из
    /// OnSourceInitialized или позже.
    /// </summary>
    public static void ApplyDarkTitleBar(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return; // HWND ещё не создан

        int value = dark ? 1 : 0;

        // Пробуем новый атрибут; если не сработал (старая сборка Win10) — старый.
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
    }
}
