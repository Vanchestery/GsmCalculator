using System.Globalization;
using System.Windows;
using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class LocalizationService : ILocalizationService
{
    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Russian;

    public event EventHandler? LanguageChanged;

    public void SetLanguage(AppLanguage language)
    {
        if (language == CurrentLanguage) return;

        CurrentLanguage = language;
        ApplyCulture(language);
        ApplyResourceDictionary(language);

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        // TryFindResource ищет по всей иерархии ресурсов, включая
        // подключённый языковой словарь. В unit-тестах Application нет —
        // тогда возвращаем сам ключ.
        var value = Application.Current?.TryFindResource(key) as string;
        return value ?? key;
    }

    public string GetFormat(string key, params object[] args)
        => string.Format(Get(key), args);

    /// <summary>
    /// Меняет CultureInfo текущего потока — влияет на форматирование
    /// дат/чисел, если где-то используется текущая культура.
    /// </summary>
    private static void ApplyCulture(AppLanguage language)
    {
        var culture = language switch
        {
            AppLanguage.Russian => new CultureInfo("ru-RU"),
            AppLanguage.English => new CultureInfo("en-US"),
            _ => CultureInfo.InvariantCulture
        };

        Thread.CurrentThread.CurrentUICulture = culture;
    }

    /// <summary>
    /// Подменяет языковой ResourceDictionary в App.Current.Resources.
    /// Использует Source-based словарь (как ThemeService) — иначе при второй
    /// смене языка старый словарь не находится для удаления (у него Source == null).
    /// </summary>
    private static void ApplyResourceDictionary(AppLanguage language)
    {
        if (Application.Current is null) return; // в unit-тестах WPF не запущен

        var fileName = language switch
        {
            AppLanguage.Russian => "Strings.ru.xaml",
            AppLanguage.English => "Strings.en.xaml",
            _ => "Strings.ru.xaml"
        };

        var uri = new Uri($"/GsmCalculator;component/Resources/{fileName}", UriKind.Relative);
        var newDict = new ResourceDictionary { Source = uri };

        var dicts = Application.Current.Resources.MergedDictionaries;

        // Удаляем ранее подключённый языковой словарь (ищем по "Strings." в пути).
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? string.Empty;
            if (src.Contains("Strings.", StringComparison.OrdinalIgnoreCase))
                dicts.RemoveAt(i);
        }

        dicts.Add(newDict);
    }
}
