using System.Windows;

namespace GsmCalculator.Helpers;

/// <summary>
/// Attached property для передачи радиуса скругления в шаблон кнопки.
///
/// Шаблон кнопки (ControlStyles.xaml) читает это значение через биндинг,
/// поэтому радиус можно задавать индивидуально в каждом стиле:
///   <Setter Property="helpers:ButtonProps.CornerRadius" Value="3"/>
///
/// Значение по умолчанию — 7 (большие кнопки калькулятора).
/// Узкие кнопки (верхняя панель, OK/Отмена, виджет) переопределяют на 3.
/// </summary>
public static class ButtonProps
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(ButtonProps),
            new PropertyMetadata(new CornerRadius(7)));

    public static CornerRadius GetCornerRadius(DependencyObject obj)
        => (CornerRadius)obj.GetValue(CornerRadiusProperty);

    public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        => obj.SetValue(CornerRadiusProperty, value);
}
