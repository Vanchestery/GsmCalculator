namespace GsmCalculator.Models;

/// <summary>
/// Одна запись истории вычислений (POCO).
/// Хранится в виде уже отформатированных строк:
///   Expression = "1250 × 0.84"
///   Result     = "1050"
/// Чтобы вывести: "{Expression} = {Result}" → "1250 × 0.84 = 1050".
///
/// Решение хранить строки целиком (а не left/op/right) принято намеренно:
/// 1) проще отображать без конвертеров,
/// 2) поддерживает «составные» строки от виджетов (например "1250 л × 0.84 = 1050 кг"),
/// 3) проще сериализовать в JSON.
/// </summary>
public class HistoryEntry
{
    public string Expression { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Удобный фабричный метод, чтобы не повторять new HistoryEntry { ... } по всему коду.
    /// </summary>
    public static HistoryEntry Create(string expression, string result)
        => new() { Expression = expression, Result = result, Timestamp = DateTime.Now };

    /// <summary>Готовая строка для отображения в UI.</summary>
    public override string ToString() => $"{Expression} = {Result}";
}
