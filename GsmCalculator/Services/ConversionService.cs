namespace GsmCalculator.Services;

/// <inheritdoc/>
public class ConversionService : IConversionService
{
    public double LitersToKilograms(double liters, double density)
    {
        // ArgumentException без nameof — иначе .NET добавляет в Message
        // суффикс " (Parameter 'density')", который мы потом показываем
        // в UI виджета. Для конечного пользователя это шум.
        if (density <= 0)
            throw new ArgumentException("Плотность должна быть больше нуля.");
        return liters * density;
    }

    public double KilogramsToLiters(double kilograms, double density)
    {
        if (density <= 0)
            throw new ArgumentException("Плотность должна быть больше нуля.");
        return kilograms / density;
    }

    public double Round(double value, int decimalPlaces)
    {
        // Ограничение 0..3 по ТЗ. Если кто-то передаст 5 — обрежем до 3,
        // отрицательное — поднимем до 0. Это безопасное поведение для
        // вызова из UI; жёсткие проверки оставляем для ViewModel.
        var dp = Math.Clamp(decimalPlaces, 0, 3);
        return Math.Round(value, dp, MidpointRounding.AwayFromZero);
    }
}
