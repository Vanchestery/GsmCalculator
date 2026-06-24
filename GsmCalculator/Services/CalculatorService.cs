using System.Globalization;

namespace GsmCalculator.Services;

/// <inheritdoc/>
public class CalculatorService : ICalculatorService
{
    public double Add(double a, double b) => a + b;
    public double Subtract(double a, double b) => a - b;
    public double Multiply(double a, double b) => a * b;

    public double Divide(double a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException("Деление на ноль запрещено.");
        return a / b;
    }

    public double Apply(double a, char op, double b) => op switch
    {
        '+' => Add(a, b),
        '-' => Subtract(a, b),
        '×' or '*' => Multiply(a, b),
        '÷' or '/' => Divide(a, b),
        _ => throw new ArgumentException($"Неизвестная операция: {op}", nameof(op))
    };

    public double ResolvePercent(double left, char op, double rightPercent) => op switch
    {
        // Для +/- процент берётся от левого операнда:
        // 100 + 10% → 100 + (100 × 0.1) = 100 + 10 = 110.
        '+' or '-' => left * (rightPercent / 100.0),

        // Для ×/÷ процент = просто rightPercent/100:
        // 100 × 10% → 100 × 0.1 = 10.
        // 100 ÷ 10% → 100 ÷ 0.1 = 1000.
        '×' or '*' or '÷' or '/' => rightPercent / 100.0,

        // Незнакомый оператор — простое value/100. На практике сюда не попадаем,
        // но без default switch не компилируется.
        _ => rightPercent / 100.0
    };

    public string FormatNumber(double value)
    {
        // NaN/Infinity — показываем явно.
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "Ошибка";

        // «Почти целое» (после ε≤1e-12) — выводим без точки.
        // Это убирает артефакты типа 5.0000000000001 → 5.
        // Также проверяем диапазон long — для очень больших чисел
        // даже целое не уместится, идём в G15.
        if (Math.Abs(value - Math.Truncate(value)) < 1e-12
            && value >= long.MinValue && value <= long.MaxValue)
        {
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        }

        // G15 = 15 значащих цифр. Это безопасная точность double:
        // дальше идут шумы IEEE 754 (например 122.52000000000001 → "122.52").
        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    public string FormatExpression(double left, char op, double right)
        => $"{FormatNumber(left)} {op} {FormatNumber(right)}";
}
