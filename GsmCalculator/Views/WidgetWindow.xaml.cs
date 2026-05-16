using System;
using System.Windows;
using GsmCalculator;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Views;

/// <summary>
/// Code-behind окна-виджета.
/// Перетаскивание за заголовок и закрытие — поведение нативной рамки
/// ToolWindow. Логика конвертации — во WidgetViewModel через команды.
/// </summary>
public partial class WidgetWindow : Window
{
    public WidgetWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var theme = App.Services?.GetService<IThemeService>()?.CurrentTheme ?? ColorTheme.Light;
        TitleBarHelper.ApplyDarkTitleBar(this, theme == ColorTheme.Dark);
    }
}
