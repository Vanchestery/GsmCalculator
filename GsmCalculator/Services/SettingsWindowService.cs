using System.Windows;
using GsmCalculator.ViewModels;
using GsmCalculator.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class SettingsWindowService : ISettingsWindowService
{
    // IServiceProvider — чтобы лениво резолвить MainViewModel (избегаем цикла DI).
    private readonly IServiceProvider _sp;

    public SettingsWindowService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void OpenDialog()
    {
        var vm = new SettingsViewModel(
            _sp.GetRequiredService<ISettingsService>(),
            _sp.GetRequiredService<IThemeService>(),
            _sp.GetRequiredService<ILocalizationService>(),
            _sp.GetRequiredService<MainViewModel>());

        var window = new SettingsWindow
        {
            DataContext = vm,
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
    }
}
