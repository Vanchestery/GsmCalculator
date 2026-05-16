using System.Linq;
using System.Windows;
using GsmCalculator.ViewModels;
using GsmCalculator.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class CreateWidgetWindowService : ICreateWidgetWindowService
{
    private readonly IServiceProvider _sp;

    public CreateWidgetWindowService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public bool OpenDialog()
    {
        var vm = new CreateWidgetViewModel(
            _sp.GetRequiredService<IWidgetService>(),
            _sp.GetRequiredService<ILocalizationService>());

        var window = new CreateWidgetWindow
        {
            DataContext = vm,
            // Владелец — окно «Добавить виджет», если оно открыто, иначе главное.
            Owner = Application.Current.Windows.OfType<AddWidgetWindow>().FirstOrDefault()
                    ?? Application.Current.MainWindow
        };

        var created = false;
        vm.CloseRequested += (_, ok) =>
        {
            created = ok;
            window.Close();
        };

        window.ShowDialog();
        return created;
    }
}
