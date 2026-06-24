using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// ViewModel окна «Добавить виджет».
/// Показывает список всех виджетов (встроенные + пользовательские).
/// Позволяет открыть выбранный виджет, создать новый, удалить свой,
/// закрепить/открепить в панели «Избранное» (v1.2).
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
    private readonly IFavoritesService _favorites;

    public ObservableCollection<WidgetListItem> Widgets { get; } = new();

    private WidgetListItem? _selectedWidget;
    public WidgetListItem? SelectedWidget
    {
        get => _selectedWidget;
        set
        {
            if (SetProperty(ref _selectedWidget, value))
                OnPropertyChanged(nameof(FavoriteToggleLabel));
        }
    }

    /// <summary>
    /// Локализованная подпись кнопки «★» — «В избранное» / «Убрать из избранного».
    /// Меняется в зависимости от текущего состояния выбранного виджета.
    /// </summary>
    public string FavoriteToggleLabel
    {
        get
        {
            if (SelectedWidget == null)
                return _loc.Get("AddWidget_AddToFavorites");
            return _loc.Get(_favorites.IsFavorite(SelectedWidget.Widget.Id)
                ? "AddWidget_RemoveFromFavorites"
                : "AddWidget_AddToFavorites");
        }
    }

    public ICommand OpenWidgetCommand { get; }
    public ICommand EditWidgetCommand { get; }
    public ICommand CreateWidgetCommand { get; }
    public ICommand DeleteWidgetCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

    public AddWidgetViewModel(
        IWidgetService widgetService,
        IWidgetWindowService widgetWindows,
        ICreateWidgetWindowService createWidget,
        ILocalizationService localization,
        IFavoritesService favorites)
    {
        _widgetService = widgetService;
        _widgetWindows = widgetWindows;
        _createWidget = createWidget;
        _loc = localization;
        _favorites = favorites;

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

        ToggleFavoriteCommand = new RelayCommand(
            _ => ToggleFavorite(),
            _ => SelectedWidget != null);

        // Список содержит локализованные подписи — пересобираем при смене языка.
        _loc.LanguageChanged += OnLanguageChanged;
        _favorites.FavoritesChanged += OnFavoritesChanged;
    }

    /// <summary>Перечитывает список виджетов из сервиса (после создания/удаления/смены языка).</summary>
    private void RefreshList()
    {
        var previouslySelectedId = SelectedWidget?.Widget.Id;

        Widgets.Clear();
        foreach (var w in _widgetService.GetAll())
            Widgets.Add(new WidgetListItem(w, _loc, _favorites.IsFavorite(w.Id)));

        // Пытаемся восстановить выделение.
        if (previouslySelectedId != null)
            SelectedWidget = Widgets.FirstOrDefault(i => i.Widget.Id == previouslySelectedId);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshList();
        OnPropertyChanged(nameof(FavoriteToggleLabel));
    }

    private void OnFavoritesChanged(object? sender, EventArgs e)
    {
        // Состав избранного изменился — обновляем индикаторы «★» в списке
        // и подпись кнопки «В избранное / Убрать».
        RefreshList();
        OnPropertyChanged(nameof(FavoriteToggleLabel));
    }

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
        // Удаление виджета убирает его из избранного — иначе зависший Id засорял бы settings.
        _favorites.Remove(item.Widget.Id);
        RefreshList();
    }

    private void ToggleFavorite()
    {
        if (SelectedWidget == null) return;
        _favorites.Toggle(SelectedWidget.Widget.Id);
        // RefreshList сработает через FavoritesChanged-подписку.
    }

    public void Dispose()
    {
        _loc.LanguageChanged -= OnLanguageChanged;
        _favorites.FavoritesChanged -= OnFavoritesChanged;
    }
}
