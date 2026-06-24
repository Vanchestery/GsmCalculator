using GsmCalculator.ViewModels;
using GsmCalculator.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class AddWidgetWindowService : IAddWidgetWindowService
{
    private readonly IServiceProvider _sp;

    // Единственный экземпляр окна — чтобы не плодить дубликаты списка.
    private AddWidgetWindow? _window;

    public AddWidgetWindowService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void OpenDialog()
    {
        // Уже открыто — просто вытаскиваем поверх.
        if (_window != null)
        {
            if (!_window.IsVisible) _window.Show();
            _window.Activate();
            return;
        }

        var vm = new AddWidgetViewModel(
            _sp.GetRequiredService<IWidgetService>(),
            _sp.GetRequiredService<IWidgetWindowService>(),
            _sp.GetRequiredService<ICreateWidgetWindowService>(),
            _sp.GetRequiredService<ILocalizationService>(),
            _sp.GetRequiredService<IFavoritesService>());

        _window = new AddWidgetWindow { DataContext = vm };
        _window.Closed += (_, _) =>
        {
            _window = null;
            vm.Dispose(); // отписка от LanguageChanged
        };
        _window.Show();
    }
}
