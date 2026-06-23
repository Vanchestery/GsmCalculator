using GsmCalculator.Helpers;
using GsmCalculator.Models;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на RoundingFormatter — чистая статическая логика округления,
/// используется MainViewModel (на =), вставкой из виджета и копированием.
/// </summary>
public class RoundingFormatterTests
{
    // ----------------------------------------------------------------
    // Apply
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(123.456, 123.456)]
    [InlineData(-0.001, -0.001)]
    public void Apply_NoneMode_ReturnsValueUnchanged(double input, double expected)
    {
        Assert.Equal(expected, RoundingFormatter.Apply(input, RoundingMode.None));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.4, 0.0)]
    [InlineData(0.6, 1.0)]
    [InlineData(1.5, 2.0)]   // банковское округление: к чётному
    [InlineData(2.5, 2.0)]   // банковское округление: к чётному
    [InlineData(-1.5, -2.0)] // к чётному в обе стороны
    [InlineData(123.45, 123.0)]
    [InlineData(150.7, 151.0)]
    public void Apply_IntegerMode_RoundsToNearestEven(double input, double expected)
    {
        Assert.Equal(expected, RoundingFormatter.Apply(input, RoundingMode.Integer));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.04, 0.0)]
    [InlineData(0.05, 0.0)]    // round-half-to-even: 0.05 → 0.0 (ближе к чётной 0)
    [InlineData(0.15, 0.2)]    // round-half-to-even: 0.15 → 0.2 (ближе к чётной 0.2)
    [InlineData(0.06, 0.1)]
    [InlineData(123.456, 123.5)]
    [InlineData(123.44, 123.4)]
    public void Apply_OneTenthMode_RoundsToOneDecimal(double input, double expected)
    {
        Assert.Equal(expected, RoundingFormatter.Apply(input, RoundingMode.OneTenth));
    }

    [Fact]
    public void Apply_NaN_ReturnsNaN()
    {
        Assert.True(double.IsNaN(RoundingFormatter.Apply(double.NaN, RoundingMode.Integer)));
    }

    [Fact]
    public void Apply_Infinity_ReturnsInfinity()
    {
        Assert.True(double.IsPositiveInfinity(
            RoundingFormatter.Apply(double.PositiveInfinity, RoundingMode.Integer)));
        Assert.True(double.IsNegativeInfinity(
            RoundingFormatter.Apply(double.NegativeInfinity, RoundingMode.OneTenth)));
    }

    // ----------------------------------------------------------------
    // Next (цикл)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(RoundingMode.None,     RoundingMode.Integer)]
    [InlineData(RoundingMode.Integer,  RoundingMode.OneTenth)]
    [InlineData(RoundingMode.OneTenth, RoundingMode.None)]
    public void Next_FollowsCycle(RoundingMode current, RoundingMode expected)
    {
        Assert.Equal(expected, RoundingFormatter.Next(current));
    }

    [Fact]
    public void Next_ThreeIterations_ReturnsToStart()
    {
        var mode = RoundingMode.None;
        mode = RoundingFormatter.Next(mode);
        mode = RoundingFormatter.Next(mode);
        mode = RoundingFormatter.Next(mode);
        Assert.Equal(RoundingMode.None, mode);
    }

    // ----------------------------------------------------------------
    // Indicator (текст на кнопке)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData(RoundingMode.None,     "∞")]
    [InlineData(RoundingMode.Integer,  "1")]
    [InlineData(RoundingMode.OneTenth, "0.1")]
    public void Indicator_ReturnsExpectedLabel(RoundingMode mode, string expected)
    {
        Assert.Equal(expected, RoundingFormatter.Indicator(mode));
    }
}
