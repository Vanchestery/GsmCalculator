using System.Windows;
using GsmCalculator.Helpers;
using Xunit;

namespace GsmCalculator.Tests;

public class WindowPositionHelperTests
{
    [Theory]
    [InlineData(-32000, -32000)]
    [InlineData(-32000, 100)]
    [InlineData(100, -32000)]
    [InlineData(double.NaN, 100)]
    [InlineData(100, double.NaN)]
    [InlineData(double.PositiveInfinity, 100)]
    [InlineData(100, double.NegativeInfinity)]
    public void IsRestorablePosition_GarbageCoordinates_ReturnsFalse(double left, double top)
        => Assert.False(WindowPositionHelper.IsRestorablePosition(left, top));

    [Fact]
    public void IsRestorablePosition_OnScreenPoint_ReturnsTrue()
    {
        var left = SystemParameters.VirtualScreenLeft + 150;
        var top = SystemParameters.VirtualScreenTop + 150;
        Assert.True(WindowPositionHelper.IsRestorablePosition(left, top));
    }

    [Fact]
    public void ShouldSatellitesFollowHost_Minimized_ReturnsFalse()
        => Assert.False(WindowPositionHelper.ShouldSatellitesFollowHost(
            WindowState.Minimized, 100, 100));

    [Fact]
    public void ShouldSatellitesFollowHost_NormalButOffscreen_ReturnsFalse()
        => Assert.False(WindowPositionHelper.ShouldSatellitesFollowHost(
            WindowState.Normal, -32000, -32000));

    [Fact]
    public void ShouldSatellitesFollowHost_NormalOnScreen_ReturnsTrue()
    {
        var left = SystemParameters.VirtualScreenLeft + 150;
        var top = SystemParameters.VirtualScreenTop + 150;
        Assert.True(WindowPositionHelper.ShouldSatellitesFollowHost(WindowState.Normal, left, top));
    }
}
