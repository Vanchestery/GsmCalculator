using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using Moq;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на WidgetViewModel — на v1.2 покрывают:
/// - копирование результата в буфер (Блок G).
/// Тесты конвертации (LitersToKg / KgToLiters) косвенно покрыты через
/// ConversionService unit-тесты + smoke-сценарии в приложении.
/// </summary>
public class WidgetViewModelTests
{
    private static (WidgetViewModel vm, Mock<IClipboardService> clipboard, MainViewModel mainVm) CreateSut()
    {
        var widget = new Widget
        {
            Id = Guid.NewGuid(),
            Name = "Тест",
            DensityMode = DensityMode.Variable,
            DefaultDensity = 0.75,
            DefaultDecimalPlaces = 2,
            IsBuiltIn = false
        };

        var loc = new Mock<ILocalizationService>();
        loc.Setup(l => l.GetFormat(It.IsAny<string>(), It.IsAny<object[]>()))
           .Returns("RESULT");

        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(AppSettings.CreateDefault());
        var clipboard = new Mock<IClipboardService>();

        var mainVm = new MainViewModel(
            new CalculatorService(), settings.Object,
            Mock.Of<IAddWidgetWindowService>(), Mock.Of<ISettingsWindowService>(),
            loc.Object, Mock.Of<IClipboardService>());

        var vm = new WidgetViewModel(
            widget,
            new ConversionService(),
            new CalculatorService(),
            mainVm,
            loc.Object,
            clipboard.Object);

        return (vm, clipboard, mainVm);
    }

    [Fact]
    public void CopyResultCommand_BeforeAnyConversion_DoesNothing()
    {
        var (vm, clipboard, _) = CreateSut();

        Assert.False(vm.CopyResultCommand.CanExecute(null));
        vm.CopyResultCommand.Execute(null);

        clipboard.Verify(c => c.SetText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CopyResultCommand_AfterConversion_CopiesRoundedNumberOnly()
    {
        // 100 литров × 0.75 = 75 кг. С DecimalPlaces=2 округление не меняет (75 целое).
        // В буфер должно лететь число без единиц: "75".
        var (vm, clipboard, mainVm) = CreateSut();
        mainVm.SetDisplayValue(100);

        vm.LitersToKgCommand.Execute(null);

        Assert.True(vm.CopyResultCommand.CanExecute(null));
        vm.CopyResultCommand.Execute(null);

        clipboard.Verify(c => c.SetText("75"), Times.Once);
    }

    [Fact]
    public void CopyResultCommand_RespectsDecimalPlaces()
    {
        // 13 л × 0.75 = 9.75 кг. С DecimalPlaces=1 → 9.8 (округление ConversionService).
        var (vm, clipboard, mainVm) = CreateSut();
        vm.DecimalPlaces = 1;
        mainVm.SetDisplayValue(13);

        vm.LitersToKgCommand.Execute(null);
        vm.CopyResultCommand.Execute(null);

        clipboard.Verify(c => c.SetText("9.8"), Times.Once);
    }

    [Fact]
    public void CopyResultCommand_AfterError_DoesNothing()
    {
        // Плотность 0 → ArgumentException → результата нет → CanExecute=false.
        var (vm, clipboard, mainVm) = CreateSut();
        vm.DensityText = "0";
        mainVm.SetDisplayValue(100);

        vm.LitersToKgCommand.Execute(null);

        Assert.False(vm.CopyResultCommand.CanExecute(null));
        clipboard.Verify(c => c.SetText(It.IsAny<string>()), Times.Never);
    }
}
