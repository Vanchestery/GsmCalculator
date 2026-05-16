namespace GsmCalculator.ViewModels;

/// <summary>
/// Пара «значение + отображаемое имя» для биндинга enum'ов в ComboBox.
/// ComboBox показывает Label, а SelectedValue отдаёт Value.
/// Это избавляет от написания EnumToString-конвертеров.
/// </summary>
public record NamedOption<T>(T Value, string Label);
