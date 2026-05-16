using System.Windows;
using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class ThemeService : IThemeService
{
    public ColorTheme CurrentTheme { get; private set; } = ColorTheme.Light;

    public void ApplyTheme(ColorTheme theme)
    {
        CurrentTheme = theme;

        // В unit-тестах Application не запущен — просто выходим.
        if (Application.Current is null) return;

        var fileName = theme switch
        {
            ColorTheme.Light => "LightTheme.xaml",
            ColorTheme.Dark => "DarkTheme.xaml",
            ColorTheme.Blue => "BlueTheme.xaml",
            _ => "LightTheme.xaml"
        };

        // Source с component-путём — надёжный способ сослаться на ResourceDictionary
        // внутри текущей сборки. WPF сам загрузит BAML.
        var uri = new Uri($"/GsmCalculator;component/Resources/Themes/{fileName}", UriKind.Relative);
        var newTheme = new ResourceDictionary { Source = uri };

        var dicts = Application.Current.Resources.MergedDictionaries;

        // Удаляем все ранее добавленные тема-словари (ищем по пути /Themes/).
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? string.Empty;
            if (src.Contains("/Themes/", StringComparison.OrdinalIgnoreCase))
                dicts.RemoveAt(i);
        }

        dicts.Add(newTheme);

        // Обновляем нативные полосы заголовка всех открытых окон.
        var isDark = theme == ColorTheme.Dark;
        foreach (Window window in Application.Current.Windows)
            TitleBarHelper.ApplyDarkTitleBar(window, isDark);
    }
}
