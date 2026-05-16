using System;
using System.Windows;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator;

/// <summary>
/// Code-behind главного окна.
/// Логика во MainViewModel; здесь только применение тёмной/светлой
/// полосы заголовка через DWM при создании окна.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // HWND уже создан — можно красить заголовок под текущую тему.
        var theme = App.Services?.GetService<IThemeService>()?.CurrentTheme ?? ColorTheme.Light;
        TitleBarHelper.ApplyDarkTitleBar(this, theme == ColorTheme.Dark);
    }
}
