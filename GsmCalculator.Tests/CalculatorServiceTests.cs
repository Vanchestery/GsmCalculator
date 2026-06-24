using System;
using GsmCalculator.Services;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на CalculatorService — чистая логика без зависимостей,
/// поэтому Moq не нужен.
/// </summary>
public class CalculatorServiceTests
{
    private readonly CalculatorService _sut = new();

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(-2, 3, 1)]
    [InlineData(0, 0, 0)]
    [InlineData(-5, -5, -10)]
    public void Add_ReturnsSum(double a, double b, double expected)
        => Assert.Equal(expected, _sut.Add(a, b));

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(3, 5, -2)]
    [InlineData(-5, -3, -2)]
    public void Subtract_ReturnsDifference(double a, double b, double expected)
        => Assert.Equal(expected, _sut.Subtract(a, b));

    [Theory]
    [InlineData(4, 3, 12)]
    [InlineData(-2, 3, -6)]
    [InlineData(5, 0, 0)]
    public void Multiply_ReturnsProduct(double a, double b, double expected)
        => Assert.Equal(expected, _sut.Multiply(a, b));

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(7, 2, 3.5)]
    [InlineData(-10, 2, -5)]
    public void Divide_ReturnsQuotient(double a, double b, double expected)
        => Assert.Equal(expected, _sut.Divide(a, b));

    [Fact]
    public void Divide_ByZero_Throws()
        => Assert.Throws<DivideByZeroException>(() => _sut.Divide(5, 0));

    // ----------------------------------------------------------------
    // ResolvePercent (Блок F v1.2): контекстный процент Windows Calc Standard
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(100, '+', 10, 10)]    // 10% от 100 = 10 (далее 100 + 10 = 110)
    [InlineData(100, '-', 10, 10)]    // 10% от 100 = 10 (далее 100 − 10 = 90)
    [InlineData(200, '+', 25, 50)]    // 25% от 200 = 50
    [InlineData(50, '-', 20, 10)]     // 20% от 50 = 10
    public void ResolvePercent_PlusMinus_ReturnsPercentOfLeft(double left, char op, double right, double expected)
        => Assert.Equal(expected, _sut.ResolvePercent(left, op, right));

    [Theory]
    [InlineData(100, '×', 10, 0.1)]   // 10% = 0.1 (далее 100 × 0.1 = 10)
    [InlineData(100, '*', 10, 0.1)]   // ASCII '*' = эквивалент '×'
    [InlineData(100, '÷', 10, 0.1)]   // 10% = 0.1 (далее 100 ÷ 0.1 = 1000)
    [InlineData(100, '/', 10, 0.1)]   // ASCII '/'
    [InlineData(50, '×', 200, 2.0)]   // 200% = 2.0
    public void ResolvePercent_MulDiv_ReturnsRightDividedBy100(double left, char op, double right, double expected)
        => Assert.Equal(expected, _sut.ResolvePercent(left, op, right));

    [Theory]
    [InlineData('?', 10, 0.1)]        // незнакомая операция — fallback на /100
    public void ResolvePercent_UnknownOp_ReturnsRightDividedBy100(char op, double right, double expected)
        => Assert.Equal(expected, _sut.ResolvePercent(100, op, right));

    [Theory]
    [InlineData(5, '+', 3, 8)]
    [InlineData(5, '-', 3, 2)]
    [InlineData(5, '×', 3, 15)]
    [InlineData(15, '÷', 3, 5)]
    [InlineData(5, '*', 3, 15)]       // ASCII-альтернатива
    [InlineData(15, '/', 3, 5)]       // ASCII-альтернатива
    public void Apply_DispatchesByOperator(double a, char op, double b, double expected)
        => Assert.Equal(expected, _sut.Apply(a, op, b));

    [Fact]
    public void Apply_UnknownOperator_Throws()
        => Assert.Throws<ArgumentException>(() => _sut.Apply(1, '?', 2));

    [Fact]
    public void Apply_DivideByZero_Throws()
        => Assert.Throws<DivideByZeroException>(() => _sut.Apply(5, '÷', 0));

    [Theory]
    [InlineData(5.0, "5")]
    [InlineData(-3.0, "-3")]
    [InlineData(1050.0, "1050")]
    [InlineData(0.84, "0.84")]
    [InlineData(0, "0")]
    public void FormatNumber_FormatsCorrectly(double value, string expected)
        => Assert.Equal(expected, _sut.FormatNumber(value));

    [Fact]
    public void FormatNumber_HidesFloatingPointArtifact()
    {
        // 45.52 + 77 в IEEE 754 даёт 122.52000000000001 — фикс G15 должен показать "122.52"
        Assert.Equal("122.52", _sut.FormatNumber(45.52 + 77));
    }

    [Fact]
    public void FormatNumber_NaN_ReturnsErrorLiteral()
        => Assert.Equal("Ошибка", _sut.FormatNumber(double.NaN));

    [Fact]
    public void FormatNumber_Infinity_ReturnsErrorLiteral()
        => Assert.Equal("Ошибка", _sut.FormatNumber(double.PositiveInfinity));

    [Fact]
    public void FormatExpression_BuildsExpressionString()
        => Assert.Equal("1250 × 0.84", _sut.FormatExpression(1250, '×', 0.84));
}
