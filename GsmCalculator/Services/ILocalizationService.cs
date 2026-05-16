using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Переключение языка интерфейса на лету (русский/английский).
///
/// Надписи в XAML переключаются через {DynamicResource} автоматически
/// при подмене языкового словаря. Строки, формируемые в коде
/// (сообщения, результаты), берутся через Get / GetFormat.
/// </summary>
public interface ILocalizationService
{
    AppLanguage CurrentLanguage { get; }

    /// <summary>Сменить язык. Если язык тот же — событие не вызывается.</summary>
    void SetLanguage(AppLanguage language);

    /// <summary>Срабатывает после смены языка. Долгоживущие ViewModels
    /// подписываются, чтобы перегенерировать свои строки.</summary>
    event EventHandler? LanguageChanged;

    /// <summary>
    /// Получить локализованную строку по ключу из текущего языкового словаря.
    /// Если ключ не найден — возвращает сам ключ (видно что забыли перевод).
    /// </summary>
    string Get(string key);

    /// <summary>
    /// Получить локализованную строку-формат и подставить аргументы.
    /// Например: GetFormat("Widget_ResultLtoKg", "1250", "0.75", "938").
    /// </summary>
    string GetFormat(string key, params object[] args);
}
