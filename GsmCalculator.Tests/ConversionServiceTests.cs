using System;
using GsmCalculator.Services;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на ConversionService — конвертация л↔кг и округление.
/// </summary>
public class ConversionServiceTests
{
    private readonly ConversionService _sut = new();

    [Theory]
    [InlineData(1000, 0.84, 840)]
    [InlineData(1250, 0.75, 937.5)]
    [InlineData(100, 1.0, 100)]
    public void LitersToKilograms_Multiplies(double liters, double density, double expected)
        => Assert.Equal(expected, _sut.LitersToKilograms(liters, density), 6);

    [Theory]
    [InlineData(840, 0.84, 1000)]
    [InlineData(937.5, 0.75, 1250)]
    public void KilogramsToLiters_Divides(double kg, double density, double expected)
        => Assert.Equal(expected, _sut.KilogramsToLiters(kg, density), 6);

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(-100)]
    public void LitersToKilograms_NonPositiveDensity_Throws(double density)
        => Assert.Throws<ArgumentException>(() => _sut.LitersToKilograms(100, density));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void KilogramsToLiters_NonPositiveDensity_Throws(double density)
        => Assert.Throws<ArgumentException>(() => _sut.KilogramsToLiters(100, density));

    [Theory]
    [InlineData(2.5, 0, 3.0)]      // ровно середина — округляем ОТ нуля (банковское дало бы 2)
    [InlineData(-2.5, 0, -3.0)]    // и отрицательную — тоже ОТ нуля
    [InlineData(0.844, 2, 0.84)]
    [InlineData(0.847, 2, 0.85)]
    [InlineData(123.4567, 3, 123.457)]
    [InlineData(100.0, 2, 100.0)]
    public void Round_RoundsAwayFromZero(double value, int dp, double expected)
        => Assert.Equal(expected, _sut.Round(value, dp), 6);

    [Fact]
    public void Round_DecimalPlacesAboveThree_ClampedToThree()
        => Assert.Equal(1.235, _sut.Round(1.23456, 5), 6);

    [Fact]
    public void Round_NegativeDecimalPlaces_ClampedToZero()
        => Assert.Equal(1.0, _sut.Round(1.23456, -1), 6);
}
