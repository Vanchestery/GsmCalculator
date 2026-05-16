using System;
using System.Windows;
using GsmCalculator;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Views;

/// <summary>
/// Диалог при запуске: «Продолжить прошлую сессию?».
/// Показывается модально (ShowDialog) до создания главного окна,
/// когда StartupBehavior = AlwaysAsk.
///
/// DialogResult == true  → продолжить сессию,
/// DialogResult == false → начать с нуля (кнопка «Нет» / Esc / крестик).
/// </summary>
public partial class ContinueSessionDialog : Window
{
    /// <param name="savedAtText">Готовая локализованная строка «Сохранена: …».</param>
    public ContinueSessionDialog(string savedAtText)
    {
        InitializeComponent();
        SavedAtText.Text = savedAtText;
    }

    private void OnYes(object sender, RoutedEventArgs e)
    {
        // Установка DialogResult автоматически закрывает модальное окно.
        DialogResult = true;
    }

    // Кнопка «Нет» помечена IsCancel="True" — WPF сам выставит
    // DialogResult = false и закроет окно, обработчик не нужен.

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var theme = App.Services?.GetService<IThemeService>()?.CurrentTheme ?? ColorTheme.Light;
        TitleBarHelper.ApplyDarkTitleBar(this, theme == ColorTheme.Dark);
    }
}
