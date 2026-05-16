namespace GsmCalculator.Services;

/// <summary>
/// Конвертация литры ↔ килограммы для виджетов ГСМ.
/// Формулы:
///   kg = liters × density
///   liters = kg / density
/// </summary>
public interface IConversionService
{
    /// <summary>Литры в килограммы. Плотность должна быть &gt; 0.</summary>
    double LitersToKilograms(double liters, double density);

    /// <summary>
    /// Килограммы в литры. Бросает <see cref="DivideByZeroException"/>
    /// или <see cref="ArgumentException"/> при density &lt;= 0.
    /// </summary>
    double KilogramsToLiters(double kilograms, double density);

    /// <summary>Округление до N знаков после запятой (используется виджетом для отображения).</summary>
    double Round(double value, int decimalPlaces);
}
