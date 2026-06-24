using System.Windows;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // System.Windows.Clipboard.SetText периодически бросает COMException
        // когда буфер удерживается другим процессом (Excel, RDP-клиент и т.п.).
        // По UX лучше «тихо не скопировать», чем уронить приложение.
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // Намеренно проглатываем — TODO: логирование когда будет логгер.
        }
    }
}
