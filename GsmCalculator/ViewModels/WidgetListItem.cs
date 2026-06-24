using System.Globalization;
using GsmCalculator.Models;
using GsmCalculator.Services;

namespace GsmCalculator.ViewModels;

/// <summary>
/// Обёртка над <see cref="Widget"/> для отображения в списке окна «Добавить виджет».
/// Держит отображаемые (локализованные) строки отдельно от POCO-модели Widget.
/// Создаётся заново при смене языка (AddWidgetViewModel.RefreshList).
/// </summary>
public class WidgetListItem
{
    private readonly ILocalizationService _loc;

    public Widget Widget { get; }

    public WidgetListItem(Widget widget, ILocalizationService loc, bool isFavorite = false)
    {
        Widget = widget;
        _loc = loc;
        IsFavorite = isFavorite;
    }

    public string Name => Widget.Name;

    public bool IsBuiltIn => Widget.IsBuiltIn;

    /// <summary>True если виджет закреплён в панели «Избранное» (v1.2).</summary>
    public bool IsFavorite { get; }

    /// <summary>«★» если закреплён, пустая строка иначе — для индикатора в списке.</summary>
    public string FavoriteIndicator => IsFavorite ? "★" : string.Empty;

    /// <summary>Метка вида: «встроенный» / «свой».</summary>
    public string KindLabel
        => _loc.Get(Widget.IsBuiltIn ? "WidgetItem_BuiltIn" : "WidgetItem_Custom");

    /// <summary>Описание плотности для подзаголовка в списке.</summary>
    public string DensityInfo
    {
        get
        {
            var d = Widget.DefaultDensity.ToString(CultureInfo.InvariantCulture);
            return Widget.DensityMode == DensityMode.Fixed
                ? _loc.GetFormat("WidgetItem_FixedDensity", d)
                : _loc.GetFormat("WidgetItem_VariableDensity", d);
        }
    }
}
