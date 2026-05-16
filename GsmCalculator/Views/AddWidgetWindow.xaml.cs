using System;
using System.Windows;
using System.Windows.Input;
using GsmCalculator;
using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Views;

/// <summary>
/// Code-behind окна «Добавить виджет».
/// Логика во AddWidgetViewModel; здесь — тёмная/светлая полоса заголовка
/// и обработка двойного клика по строке списка.
/// Закрытие — нативным крестиком ToolWindow.
/// </summary>
public partial class AddWidgetWindow : Window
{
    public AddWidgetWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Двойной клик по строке списка = открыть виджет.
    /// Используем событие MouseDoubleClick вместо MouseBinding — надёжнее
    /// (MouseBinding на ListBox капризно работает из-за ListBoxItem).
    /// </summary>
    private void OnWidgetDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is AddWidgetViewModel vm
            && vm.OpenWidgetCommand.CanExecute(null))
        {
            vm.OpenWidgetCommand.Execute(null);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var theme = App.Services?.GetService<IThemeService>()?.CurrentTheme ?? ColorTheme.Light;
        TitleBarHelper.ApplyDarkTitleBar(this, theme == ColorTheme.Dark);
    }
}
