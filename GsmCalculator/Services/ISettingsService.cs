using GsmCalculator.Models;

namespace GsmCalculator.Services;

/// <summary>
/// Загрузка и сохранение настроек приложения (settings.json).
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Читает settings.json. Если файла нет или он повреждён —
    /// возвращает <see cref="AppSettings.CreateDefault"/>.
    /// </summary>
    AppSettings Load();

    /// <summary>Перезаписывает settings.json новыми настройками.</summary>
    void Save(AppSettings settings);
}
