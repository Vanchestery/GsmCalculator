using System;
using System.ComponentModel;
using System.Windows;
using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GsmCalculator;

/// <summary>
/// Code-behind главного окна.
/// Логика во MainViewModel. Здесь:
/// - применение тёмной/светлой полосы заголовка через DWM при создании окна
/// - реакция на смену IsHistoryVisible (физическое скрытие колонки + сужение окна)
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>Базовая ширина окна — только калькулятор (без панелей).</summary>
    private const double MinWidthBase = 360;

    /// <summary>Ширина колонки «Избранное» в развёрнутом состоянии.</summary>
    private const double FavoritesColumnWidth = 150;

    /// <summary>Минимальная ширина колонки истории в развёрнутом состоянии.</summary>
    private const double HistoryColumnMinWidth = 180;

    /// <summary>Отступ между колонками (Border.Margin).</summary>
    private const double InterColumnSpacing = 10;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // На первом запуске App.OnStartup ставит SizeToContent="WidthAndHeight"
        // чтобы окно село по содержимому. Сразу после рендера возвращаем Manual —
        // иначе пользователь не сможет менять размер мышкой.
        if (SizeToContent != SizeToContent.Manual)
            SizeToContent = SizeToContent.Manual;

        // Если сохранённая ширина явно великовата для текущего состава панелей
        // (наследие старой версии, или юзер закрыл с историей а открыл без неё) —
        // пересчитать один раз по содержимому.
        if (DataContext is MainViewModel vm)
        {
            var targetMin = ComputeMinWidth(vm.IsFavoritesVisible, vm.IsHistoryVisible);
            if (Width > targetMin * 1.5)
            {
                SizeToContent = SizeToContent.Width;
                SizeToContent = SizeToContent.Manual;
            }
        }
    }

    /// <summary>
    /// Считает MinWidth для текущего набора видимых панелей.
    /// База — 360 (калькулятор). Каждая видимая панель добавляет свою ширину + спейсинг.
    /// </summary>
    private static double ComputeMinWidth(bool favsVisible, bool historyVisible)
    {
        var w = MinWidthBase;
        if (favsVisible) w += FavoritesColumnWidth + InterColumnSpacing;
        if (historyVisible) w += HistoryColumnMinWidth + InterColumnSpacing;
        return w;
    }

    /// <summary>
    /// Подписываемся на PropertyChanged ViewModel чтобы реагировать на toggle.
    /// DataContext назначается из App.OnStartup до Show — этот обработчик
    /// сработает один раз с актуальной VM.
    /// </summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Реагируем на изменения видимости любой из двух панелей.
        if (e.PropertyName is not (nameof(MainViewModel.IsHistoryVisible)
            or nameof(MainViewModel.IsFavoritesVisible))) return;
        if (DataContext is not MainViewModel vm) return;
        ApplyPanelsVisibility(vm.IsFavoritesVisible, vm.IsHistoryVisible);
    }

    /// <summary>
    /// Физически меняет layout: схлопывает/раскрывает колонки и Border'ы
    /// панелей «Избранное» и «История», пересчитывает MinWidth окна,
    /// потом одноразово ставит SizeToContent=Width чтобы WPF сама перемерила
    /// окно по содержимому.
    ///
    /// Важно ВНАЧАЛЕ снизить MinWidth окна — иначе SizeToContent упрётся в него
    /// и не сможет сжать окно ниже него.
    /// </summary>
    private void ApplyPanelsVisibility(bool favsVisible, bool historyVisible)
    {
        // Favorites column
        if (favsVisible)
        {
            FavoritesColumn.MinWidth = FavoritesColumnWidth;
            FavoritesColumn.Width = new GridLength(FavoritesColumnWidth);
            FavoritesBorder.Visibility = Visibility.Visible;
        }
        else
        {
            FavoritesColumn.MinWidth = 0;
            FavoritesColumn.Width = new GridLength(0);
            FavoritesBorder.Visibility = Visibility.Collapsed;
        }

        // History column
        if (historyVisible)
        {
            HistoryColumn.MinWidth = HistoryColumnMinWidth;
            HistoryColumn.Width = new GridLength(1, GridUnitType.Star);
            HistoryBorder.Visibility = Visibility.Visible;
        }
        else
        {
            HistoryColumn.MinWidth = 0;
            HistoryColumn.Width = new GridLength(0);
            HistoryBorder.Visibility = Visibility.Collapsed;
        }

        MinWidth = ComputeMinWidth(favsVisible, historyVisible);

        if (IsLoaded)
        {
            SizeToContent = SizeToContent.Width;
            SizeToContent = SizeToContent.Manual;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // HWND уже создан — можно красить заголовок под текущую тему.
        var theme = App.Services?.GetService<IThemeService>()?.CurrentTheme ?? ColorTheme.Light;
        TitleBarHelper.ApplyDarkTitleBar(this, theme == ColorTheme.Dark);
    }
}
