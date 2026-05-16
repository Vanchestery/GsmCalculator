using System.Text.Json;
using System.Text.Json.Serialization;

namespace GsmCalculator.Services;

/// <summary>
/// Общие настройки сериализации JSON. Используем во всех сервисах,
/// которые читают/пишут файлы. Один и тот же формат — удобно
/// читать и редактировать вручную при отладке.
/// </summary>
internal static class JsonOptions
{
    /// <summary>
    /// WriteIndented — красивый отступ, читаемо в блокноте.
    /// JsonStringEnumConverter — enum'ы пишутся как "Light", "Russian",
    /// а не числами 0/1. Это безопаснее при изменении порядка enum'ов
    /// в коде в будущем.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };
}
