using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel окна «Добавить виджет».
/// Показывает список всех виджетов (встроенные + пользовательские).
/// Позволяет открыть выбранный виджет, создать новый или удалить свой.
/// Окно остаётся открытым после открытия виджета — можно открыть несколько.
///
/// Подписан на смену языка (перестраивает список с новыми подписями),
/// поэтому реализует IDisposable — отписка обязательна.
/// </summary>
public class AddWidgetViewModel : ViewModelBase, IDisposable
{
    private readonly IWidgetService _widgetService;
    private readonly IWidgetWindowService _widgetWindows;
    private readonly ICreateWidgetWindowService _createWidget;
    private readonly ILocalizationService _loc;

    public ObservableCollection<WidgetListItem> Widgets { get; } = new();

    private WidgetListItem? _selectedWidget;
    public WidgetListItem? SelectedWidget
    {
        get => _selectedWidget;
        set => SetProperty(ref _selectedWidget, value);
    }

    public ICommand OpenWidgetCommand { get; }
    public ICommand EditWidgetCommand { get; }
    public ICommand CreateWidgetCommand { get; }
    public ICommand DeleteWidgetCommand { get; }

    public AddWidgetViewModel(
        IWidgetService widgetService,
        IWidgetWindowService widgetWindows,
        ICreateWidgetWindowService createWidget,
        ILocalizationService localization)
    {
        _widgetService = widgetService;
        _widgetWindows = widgetWindows;
        _createWidget = createWidget;
        _loc = localization;

        RefreshList();

        OpenWidgetCommand = new RelayCommand(
            _ => OpenSelected(),
            _ => SelectedWidget != null);

        // Редактировать можно ЛЮБОЙ виджет (включая встроенные —
        // их IsBuiltIn-флаг защищён на уровне WidgetService.Update).
        EditWidgetCommand = new RelayCommand(
            _ => EditSelected(),
            _ => SelectedWidget != null);

        CreateWidgetCommand = new RelayCommand(_ => CreateNew());

        DeleteWidgetCommand = new RelayCommand(
            _ => DeleteSelected(),
            // Удалять можно только пользовательские виджеты.
            _ => SelectedWidget != null && !SelectedWidget.IsBuiltIn);

        // Список содержит локализованные подписи — пересобираем при смене языка.
        _loc.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>Перечитывает список виджетов из сервиса (после создания/удаления/смены языка).</summary>
    private void RefreshList()
    {
        var previouslySelectedId = SelectedWidget?.Widget.Id;

        Widgets.Clear();
        foreach (var w in _widgetService.GetAll())
            Widgets.Add(new WidgetListItem(w, _loc));

        // Пытаемся восстановить выделение.
        if (previouslySelectedId != null)
            SelectedWidget = Widgets.FirstOrDefault(i => i.Widget.Id == previouslySelectedId);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshList();

    private void OpenSelected()
    {
        if (SelectedWidget != null)
            _widgetWindows.OpenOrFocus(SelectedWidget.Widget);
    }

    private void CreateNew()
    {
        // Модальный диалог создания. Вернёт true, если виджет создан.
        if (_createWidget.OpenDialog())
            RefreshList();
    }

    private void EditSelected()
    {
        var item = SelectedWidget;
        if (item == null) return;

        // Тот же диалог что Create, но с префиллом из выбранного виджета.
        if (_createWidget.OpenDialog(item.Widget))
            RefreshList();
    }

    private void DeleteSelected()
    {
        var item = SelectedWidget;
        if (item == null || item.IsBuiltIn) return;

        // Подтверждение перед удалением.
        // (В «чистом» MVVM здесь был бы IDialogService; для нашего масштаба
        //  прямой MessageBox — приемлемый прагматичный компромисс.)
        var result = MessageBox.Show(
            _loc.GetFormat("AddWidget_DeleteConfirm", item.Name),
            _loc.Get("AddWidget_DeleteTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _widgetService.Remove(item.Widget.Id);
        RefreshList();
    }

    public void Dispose()
    {
        _loc.LanguageChanged -= OnLanguageChanged;
    }
}
