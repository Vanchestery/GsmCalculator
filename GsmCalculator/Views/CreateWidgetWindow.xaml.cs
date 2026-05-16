using System;
using System.Windows;
using GsmCalculator;
using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Views;

/// <summary>
/// Code-behind окна создания виджета.
/// Закрытие — по событию CloseRequested из ViewModel.
/// </summary>
public partial class CreateWidgetWindow : Window
{
    public CreateWidgetWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CreateWidgetViewModel vm)
            vm.CloseRequested += (_, _) => Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var theme = App.Services?.GetService<IThemeService>()?.CurrentTheme ?? ColorTheme.Light;
        TitleBarHelper.ApplyDarkTitleBar(this, theme == ColorTheme.Dark);
    }
}
