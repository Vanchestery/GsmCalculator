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

    /// <summary>
    /// Минимальная ширина окна в режиме без истории. Топ-бар с тремя
    /// иконками-кнопками теперь ~150px, главное ограничение — калькулятор-колонка
    /// с её MinWidth=320 плюс outer Grid Margin (по 10 с каждой стороны).
    /// </summary>
    private const double MinWidthWithoutHistory = 360;

    /// <summary>
    /// Минимальная ширина окна в режиме с историей. Учитывает калькулятор-колонку
    /// (320) + историю-колонку (180) + поля.
    /// </summary>
    private const double MinWidthWithHistory = 540;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // На первом запуске App.OnStartup ставит SizeToContent="WidthAndHeight"
        // чтобы окно село по содержимому. Сразу после рендера возвращаем Manual —
        // иначе пользователь не сможет менять размер мышкой.
        if (SizeToContent != SizeToContent.Manual)
            SizeToContent = SizeToContent.Manual;

        // Если история скрыта, но сохранённая ширина явно великовата
        // (наследие предыдущей версии, где compact-режим не умел сжиматься,
        // и окно «зависало» 540+ с растянутыми кнопками) — пересчитать
        // один раз по содержимому.
        if (DataContext is MainViewModel vm && !vm.IsHistoryVisible
            && Width > MinWidthWithoutHistory * 1.5)
        {
            SizeToContent = SizeToContent.Width;
            SizeToContent = SizeToContent.Manual;
        }
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
        if (e.PropertyName != nameof(MainViewModel.IsHistoryVisible)) return;
        if (DataContext is not MainViewModel vm) return;
        ApplyHistoryVisibility(vm.IsHistoryVisible);
    }

    /// <summary>
    /// Физически меняет layout: схлопывает колонку и Border истории,
    /// потом одноразово ставит SizeToContent=Width чтобы WPF сама перемерила
    /// окно по содержимому. Сразу возвращаем Manual чтобы пользователь
    /// мог дальше ресайзить мышкой.
    ///
    /// Важно ВНАЧАЛЕ снизить MinWidth окна — иначе SizeToContent упрётся в него
    /// и не сможет сжать окно ниже него.
    /// </summary>
    private void ApplyHistoryVisibility(bool visible)
    {
        if (visible)
        {
            HistoryColumn.MinWidth = 180;
            HistoryColumn.Width = new GridLength(1, GridUnitType.Star);
            HistoryBorder.Visibility = Visibility.Visible;
            MinWidth = MinWidthWithHistory;
        }
        else
        {
            HistoryColumn.MinWidth = 0;
            HistoryColumn.Width = new GridLength(0);
            HistoryBorder.Visibility = Visibility.Collapsed;
            MinWidth = MinWidthWithoutHistory;
        }

        // Перемеряем окно по содержимому — WPF сама вычислит новую ширину
        // (с учётом обновлённого MinWidth), нам не нужно считать вручную.
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
