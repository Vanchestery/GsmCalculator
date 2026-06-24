namespace GsmCalculator.Models;

/// <summary>
/// Сериализуемое состояние главного окна (window-state.json).
/// Хранится отдельно от <see cref="SessionState"/> — позиция/размер
/// запоминаются ВСЕГДА, независимо от настройки StartupBehavior.
///
/// Значения берутся из <c>Window.RestoreBounds</c>, который содержит
/// геометрию окна в normal-режиме (даже если оно сейчас maximized).
/// </summary>
public class MainWindowState
{
    /// <summary>Координата левого края (Window.RestoreBounds.Left).</summary>
    public double Left { get; set; }

    /// <summary>Координата верхнего края (Window.RestoreBounds.Top).</summary>
    public double Top { get; set; }

    /// <summary>Ширина в normal-режиме (Window.RestoreBounds.Width).</summary>
    public double Width { get; set; }

    /// <summary>Высота в normal-режиме (Window.RestoreBounds.Height).</summary>
    public double Height { get; set; }

    /// <summary>True если окно было развёрнуто на весь экран при закрытии.</summary>
    public bool IsMaximized { get; set; }

    /// <summary>
    /// True если панель истории была видна при закрытии. По умолчанию true.
    /// Связано с layout окна (а не с behavior), поэтому хранится здесь,
    /// а не в AppSettings.
    /// </summary>
    public bool IsHistoryVisible { get; set; } = true;

    /// <summary>
    /// True если панель «Избранное» была видна при закрытии (v1.2).
    /// По умолчанию false — пустая панель не мозолит глаза новому юзеру.
    /// </summary>
    public bool IsFavoritesVisible { get; set; } = false;
}
