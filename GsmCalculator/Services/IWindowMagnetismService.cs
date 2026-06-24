using System.Windows;

namespace GsmCalculator.Services;

/// <summary>
/// Магнитное прилипание окон-виджетов к главному окну (v1.2 — блок J).
///
/// Регистрация: главное окно — один раз через RegisterHost, виджеты —
/// при открытии через RegisterSatellite и снимаются при закрытии.
/// Сервис сам подписывается на LocationChanged и обновляет позиции.
/// </summary>
public interface IWindowMagnetismService
{
    /// <summary>
    /// Регистрирует главное окно как «хост». Может быть только один.
    /// Повторный вызов с другим Window перевешивает подписку.
    /// </summary>
    void RegisterHost(Window host);

    /// <summary>
    /// Регистрирует окно-виджет как «сателлита». При движении сателлита
    /// проверяется снэп к граням хоста. При движении хоста — все
    /// прилипшие сателлиты двигаются вместе с ним.
    /// </summary>
    void RegisterSatellite(Window satellite);

    /// <summary>
    /// Снимает регистрацию сателлита (при закрытии окна виджета).
    /// </summary>
    void UnregisterSatellite(Window satellite);
}
