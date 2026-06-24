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

    /// <summary>
    /// Контекстный процент в стиле Windows Calc Standard.
    /// Возвращает «разрешённое» значение правого операнда для процентной операции,
    /// которое потом подставляется в обычный <see cref="Apply"/>.
    ///
    /// Логика:
    /// <list type="bullet">
    /// <item><c>left + rightPercent%</c> → возвращает <c>left × (rightPercent / 100)</c>.
    ///       Пример: <c>100 + 10%</c> → returns 10, далее <c>100 + 10 = 110</c>.</item>
    /// <item><c>left − rightPercent%</c> → то же что для +. Пример: <c>100 − 10%</c> → 10 → <c>100 − 10 = 90</c>.</item>
    /// <item><c>left × rightPercent%</c> → возвращает <c>rightPercent / 100</c>.
    ///       Пример: <c>100 × 10%</c> → 0.1 → <c>100 × 0.1 = 10</c>.</item>
    /// <item><c>left ÷ rightPercent%</c> → возвращает <c>rightPercent / 100</c>.
    ///       Пример: <c>100 ÷ 10%</c> → 0.1 → <c>100 ÷ 0.1 = 1000</c>.</item>
    /// </list>
    /// Для незнакомого оператора возвращает простое <c>rightPercent / 100</c>.
    /// </summary>
    double ResolvePercent(double left, char op, double rightPercent);

    /// <summary>Форматирует число для дисплея. Целое — без точки, дробное — с точкой.</summary>
    string FormatNumber(double value);

    /// <summary>Форматирует выражение для записи в историю: "1250 × 0.84".</summary>
    string FormatExpression(double left, char op, double right);
}
