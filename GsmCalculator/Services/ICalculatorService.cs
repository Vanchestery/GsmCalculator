namespace GsmCalculator.Services;

/// <summary>
/// Операции калькулятора. Stateless — методы принимают аргументы
/// и возвращают результат. «Текущее число», «ожидающая операция»
/// и прочее состояние UI хранится в MainViewModel.
/// </summary>
public interface ICalculatorService
{
    double Add(double a, double b);
    double Subtract(double a, double b);
    double Multiply(double a, double b);

    /// <summary>Деление. Бросает <see cref="DivideByZeroException"/> при b == 0.</summary>
    double Divide(double a, double b);

    /// <summary>
    /// Универсальная точка входа для вычислений по символу операции.
    /// Используется чтобы не дублировать switch в нескольких местах.
    /// </summary>
    double Apply(double a, char op, double b);

    /// <summary>Форматирует число для дисплея. Целое — без точки, дробное — с точкой.</summary>
    string FormatNumber(double value);

    /// <summary>Форматирует выражение для записи в историю: "1250 × 0.84".</summary>
    string FormatExpression(double left, char op, double right);
}
