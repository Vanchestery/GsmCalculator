using GsmCalculator.Models;

namespace GsmCalculator.Helpers;

/// <summary>
/// Применяет режим округления (<see cref="RoundingMode"/>) к значениям калькулятора.
/// Чистая функция без состояния — легко тестируется и вызывается из нескольких мест:
/// MainViewModel (на =), вставка из виджета, копирование в буфер.
///
/// Округление — <see cref="MidpointRounding.ToEven"/> (банковское):
/// 0.5 → 0, 1.5 → 2, 2.5 → 2. Это стандарт IEEE 754 и финансовых расчётов:
/// при большом наборе округлений ошибки усреднения статистически нулевые,
/// в отличие от Math.Round(...) "AwayFromZero" где есть систематический биас.
/// </summary>
public static class RoundingFormatter
{
    /// <summary>
    /// Округляет значение согласно режиму. NaN/Infinity возвращаются без изменений
    /// (никакое округление их не «починит» — пусть форматтер дисплея покажет ошибку).
    /// </summary>
    public static double Apply(double value, RoundingMode mode)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return value;

        return mode switch
        {
            RoundingMode.Integer  => Math.Round(value, 0, MidpointRounding.ToEven),
            RoundingMode.OneTenth => Math.Round(value, 1, MidpointRounding.ToEven),
            RoundingMode.None     => value,
            _                     => value
        };
    }

    /// <summary>
    /// Следующий режим в цикле для кнопки-цикла:
    /// None → Integer → OneTenth → None → ...
    /// </summary>
    public static RoundingMode Next(RoundingMode current) => current switch
    {
        RoundingMode.None     => RoundingMode.Integer,
        RoundingMode.Integer  => RoundingMode.OneTenth,
        RoundingMode.OneTenth => RoundingMode.None,
        _                     => RoundingMode.None
    };

    /// <summary>
    /// Короткий индикатор режима для текста кнопки в топ-баре.
    /// Видно прямо на кнопке без раскрытия меню/тултипа.
    /// </summary>
    public static string Indicator(RoundingMode mode) => mode switch
    {
        RoundingMode.None     => "∞",
        RoundingMode.Integer  => "1",
        RoundingMode.OneTenth => "0.1",
        _                     => "?"
    };
}
