using System;
using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using Moq;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на MainViewModel — самая сложная логика приложения (state machine
/// калькулятора, два режима, ошибки, история).
///
/// Подход: настоящий CalculatorService (чистая логика) + Moq-заглушки
/// остальных зависимостей. ISettingsService возвращает заранее заданный
/// AppSettings, ILocalizationService возвращает «Ошибка» для ключа Common_Error.
/// </summary>
public class MainViewModelTests
{
    /// <summary>Фабрика VM с реальным калькулятором и Moq-зависимостями.</summary>
    private static MainViewModel CreateSut(
        int historySize = 10,
        CalculatorMode mode = CalculatorMode.Classic)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(new AppSettings
        {
            HistorySize = historySize,
            CalculatorMode = mode
        });

        var loc = new Mock<ILocalizationService>();
        loc.Setup(l => l.Get("Common_Error")).Returns("Ошибка");

        return new MainViewModel(
            new CalculatorService(),                  // настоящий — чистая логика
            settings.Object,
            Mock.Of<IAddWidgetWindowService>(),
            Mock.Of<ISettingsWindowService>(),
            loc.Object);
    }

    /// <summary>Вводит несколько цифр последовательно.</summary>
    private static void Enter(MainViewModel vm, string digits)
    {
        foreach (var d in digits)
            vm.DigitCommand.Execute(d.ToString());
    }

    // ----------------------------------------------------------------
    // Начальное состояние и ввод
    // ----------------------------------------------------------------

    [Fact]
    public void InitialState_DisplayIsZero()
    {
        var vm = CreateSut();
        Assert.Equal("0", vm.Display);
    }

    [Fact]
    public void InitialState_HistoryIsEmpty()
    {
        var vm = CreateSut();
        Assert.Empty(vm.History);
    }

    [Fact]
    public void Digits_AppendToDisplay()
    {
        var vm = CreateSut();
        vm.DigitCommand.Execute("7");
        vm.DigitCommand.Execute("8");
        Assert.Equal("78", vm.Display);
    }

    [Fact]
    public void Digit_ReplacesLeadingZero()
    {
        var vm = CreateSut();
        vm.DigitCommand.Execute("5");
        Assert.Equal("5", vm.Display); // не "05"
    }

    [Fact]
    public void Decimal_AddsDecimalPoint_OnlyOnce()
    {
        var vm = CreateSut();
        Enter(vm, "5");
        vm.DecimalCommand.Execute(null);
        vm.DigitCommand.Execute("2");
        vm.DecimalCommand.Execute(null); // вторая точка игнорируется
        vm.DigitCommand.Execute("7");

        Assert.Equal("5.27", vm.Display);
    }

    // ----------------------------------------------------------------
    // Classic режим
    // ----------------------------------------------------------------

    [Fact]
    public void ClassicMode_SimpleAddition()
    {
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "45");
        vm.OperationCommand.Execute("+");
        Enter(vm, "7");
        vm.EqualsCommand.Execute(null);

        Assert.Equal("52", vm.Display);
    }

    [Fact]
    public void ClassicMode_ChainedOperations_LeftToRight()
    {
        // 5 − 1 × 3 в классике = 12 (слева направо: сначала 5−1=4, потом 4×3=12)
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "5"); vm.OperationCommand.Execute("-");
        Enter(vm, "1"); vm.OperationCommand.Execute("×");
        Enter(vm, "3"); vm.EqualsCommand.Execute(null);

        Assert.Equal("12", vm.Display);
    }

    [Fact]
    public void ClassicMode_FloatingPointFix_NoTrailingArtifact()
    {
        // Проверяем фикс G15: 45.52 + 77 не должно показать 122.52000000000001
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "45");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "52");
        vm.OperationCommand.Execute("+");
        Enter(vm, "77");
        vm.EqualsCommand.Execute(null);

        Assert.Equal("122.52", vm.Display);
    }

    // ----------------------------------------------------------------
    // Engineering режим
    // ----------------------------------------------------------------

    [Fact]
    public void EngineeringMode_RespectsOperatorPrecedence()
    {
        // 5 − 1 × 3 в инженерном = 2 (× раньше −)
        var vm = CreateSut(mode: CalculatorMode.Engineering);
        Enter(vm, "5"); vm.OperationCommand.Execute("-");
        Enter(vm, "1"); vm.OperationCommand.Execute("×");
        Enter(vm, "3"); vm.EqualsCommand.Execute(null);

        Assert.Equal("2", vm.Display);
    }

    [Fact]
    public void EngineeringMode_AdditionsAreLeftToRight()
    {
        // 5 + 3 + 2 = 10 (× и ÷ нет — обычное последовательное сложение)
        var vm = CreateSut(mode: CalculatorMode.Engineering);
        Enter(vm, "5"); vm.OperationCommand.Execute("+");
        Enter(vm, "3"); vm.OperationCommand.Execute("+");
        Enter(vm, "2"); vm.EqualsCommand.Execute(null);

        Assert.Equal("10", vm.Display);
    }

    // ----------------------------------------------------------------
    // Ошибки
    // ----------------------------------------------------------------

    [Fact]
    public void DivideByZero_ShowsError()
    {
        var vm = CreateSut();
        Enter(vm, "5"); vm.OperationCommand.Execute("÷");
        Enter(vm, "0"); vm.EqualsCommand.Execute(null);

        Assert.Equal("Ошибка", vm.Display);
    }

    [Fact]
    public void DigitAfterError_StartsFreshNumber()
    {
        var vm = CreateSut();
        Enter(vm, "5"); vm.OperationCommand.Execute("÷");
        Enter(vm, "0"); vm.EqualsCommand.Execute(null);

        vm.DigitCommand.Execute("9");

        Assert.Equal("9", vm.Display);
    }

    // ----------------------------------------------------------------
    // История
    // ----------------------------------------------------------------

    [Fact]
    public void Equals_AddsEntryToHistory()
    {
        var vm = CreateSut();
        Enter(vm, "2"); vm.OperationCommand.Execute("+");
        Enter(vm, "3"); vm.EqualsCommand.Execute(null);

        Assert.Single(vm.History);
        Assert.Equal("5", vm.History[0].Result);
    }

    [Fact]
    public void History_TrimmedToHistorySize()
    {
        var vm = CreateSut(historySize: 3);
        for (int i = 0; i < 5; i++)
        {
            Enter(vm, "1"); vm.OperationCommand.Execute("+");
            Enter(vm, "1"); vm.EqualsCommand.Execute(null);
        }
        Assert.Equal(3, vm.History.Count);
    }

    [Fact]
    public void ApplyHistorySize_TrimsCurrentHistory()
    {
        var vm = CreateSut(historySize: 10);
        for (int i = 0; i < 5; i++)
        {
            Enter(vm, "1"); vm.OperationCommand.Execute("+");
            Enter(vm, "1"); vm.EqualsCommand.Execute(null);
        }
        Assert.Equal(5, vm.History.Count);

        vm.ApplyHistorySize(2);

        Assert.Equal(2, vm.History.Count);
    }

    // ----------------------------------------------------------------
    // Сервисные команды
    // ----------------------------------------------------------------

    [Fact]
    public void Clear_ResetsDisplayAndState()
    {
        var vm = CreateSut();
        Enter(vm, "123");

        vm.ClearCommand.Execute(null);

        Assert.Equal("0", vm.Display);
    }

    [Fact]
    public void Backspace_RemovesLastDigit()
    {
        var vm = CreateSut();
        Enter(vm, "123");

        vm.BackspaceCommand.Execute(null);

        Assert.Equal("12", vm.Display);
    }

    [Fact]
    public void Negate_TogglesSign()
    {
        var vm = CreateSut();
        Enter(vm, "5");

        vm.NegateCommand.Execute(null);
        Assert.Equal("-5", vm.Display);

        vm.NegateCommand.Execute(null);
        Assert.Equal("5", vm.Display);
    }

    [Fact]
    public void Negate_OnZero_DoesNothing()
    {
        var vm = CreateSut();
        vm.NegateCommand.Execute(null);
        Assert.Equal("0", vm.Display);
    }

    // ----------------------------------------------------------------
    // Публичное API для виджетов / сессии
    // ----------------------------------------------------------------

    [Fact]
    public void GetDisplayValue_ReturnsParsedNumber()
    {
        var vm = CreateSut();
        Enter(vm, "42");

        Assert.Equal(42.0, vm.GetDisplayValue());
    }

    [Fact]
    public void SetDisplayValue_UpdatesDisplay()
    {
        var vm = CreateSut();
        vm.SetDisplayValue(918.75);
        Assert.Equal("918.75", vm.Display);
    }

    [Fact]
    public void NotifyDisplayConsumed_NextDigitStartsNewNumber()
    {
        // Сценарий: пользователь ввёл 100, виджет прочитал значение
        // через GetDisplayValue, затем пользователь вводит 200.
        // Должно быть 200, а не 100200.
        var vm = CreateSut();
        Enter(vm, "100");

        _ = vm.GetDisplayValue();
        vm.NotifyDisplayConsumed();

        Enter(vm, "200");
        Assert.Equal("200", vm.Display);
    }

    [Fact]
    public void NotifyDisplayConsumed_PreservesPendingOperation()
    {
        // 5 + 100 → виджет → 200 → = должно дать 205 (5 + 200), а не 200
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "5");
        vm.OperationCommand.Execute("+");
        Enter(vm, "100");

        vm.NotifyDisplayConsumed();

        Enter(vm, "200");
        vm.EqualsCommand.Execute(null);

        Assert.Equal("205", vm.Display);
    }

    [Fact]
    public void GetSessionDisplay_AfterError_ReturnsZero()
    {
        var vm = CreateSut();
        Enter(vm, "5"); vm.OperationCommand.Execute("÷");
        Enter(vm, "0"); vm.EqualsCommand.Execute(null);

        // Display сейчас "Ошибка", но GetSessionDisplay должен вернуть "0"
        Assert.Equal("0", vm.GetSessionDisplay());
    }

    [Fact]
    public void RestoreSession_RestoresDisplayAndHistory()
    {
        var vm = CreateSut();
        var history = new[] { HistoryEntry.Create("1 + 1", "2") };

        vm.RestoreSession("777", history);

        Assert.Equal("777", vm.Display);
        Assert.Single(vm.History);
        Assert.Equal("2", vm.History[0].Result);
    }

    [Fact]
    public void RestoreSession_InvalidDisplay_FallsBackToZero()
    {
        var vm = CreateSut();
        vm.RestoreSession("not a number", Array.Empty<HistoryEntry>());

        Assert.Equal("0", vm.Display);
    }

    // ----------------------------------------------------------------
    // Toggle истории
    // ----------------------------------------------------------------

    [Fact]
    public void IsHistoryVisible_DefaultsToTrue()
    {
        var vm = CreateSut();
        Assert.True(vm.IsHistoryVisible);
    }

    [Fact]
    public void ToggleHistoryCommand_FlipsIsHistoryVisible()
    {
        var vm = CreateSut();

        vm.ToggleHistoryCommand.Execute(null);
        Assert.False(vm.IsHistoryVisible);

        vm.ToggleHistoryCommand.Execute(null);
        Assert.True(vm.IsHistoryVisible);
    }

    [Fact]
    public void HistoryToggleLabel_UsesCorrectKey_BasedOnState()
    {
        // Локальный setup loc-мока: возвращаем разные строки для разных ключей.
        var loc = new Mock<ILocalizationService>();
        loc.Setup(l => l.Get("Main_HideHistory")).Returns("HIDE");
        loc.Setup(l => l.Get("Main_ShowHistory")).Returns("SHOW");
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(AppSettings.CreateDefault());

        var vm = new MainViewModel(
            new CalculatorService(), settings.Object,
            Mock.Of<IAddWidgetWindowService>(), Mock.Of<ISettingsWindowService>(), loc.Object);

        Assert.Equal("HIDE", vm.HistoryToggleLabel);
        vm.ToggleHistoryCommand.Execute(null);
        Assert.Equal("SHOW", vm.HistoryToggleLabel);
    }

    // ----------------------------------------------------------------
    // Команды открытия окон — Moq Verify
    // ----------------------------------------------------------------

    [Fact]
    public void OpenSettingsCommand_CallsSettingsWindowService()
    {
        var settingsWindow = new Mock<ISettingsWindowService>();
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(AppSettings.CreateDefault());
        var loc = new Mock<ILocalizationService>();

        var vm = new MainViewModel(
            new CalculatorService(), settings.Object,
            Mock.Of<IAddWidgetWindowService>(), settingsWindow.Object, loc.Object);

        vm.OpenSettingsCommand.Execute(null);

        settingsWindow.Verify(s => s.OpenDialog(), Times.Once);
    }

    [Fact]
    public void OpenAddWidgetCommand_CallsAddWidgetWindowService()
    {
        var addWidget = new Mock<IAddWidgetWindowService>();
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(AppSettings.CreateDefault());
        var loc = new Mock<ILocalizationService>();

        var vm = new MainViewModel(
            new CalculatorService(), settings.Object,
            addWidget.Object, Mock.Of<ISettingsWindowService>(), loc.Object);

        vm.OpenAddWidgetCommand.Execute(null);

        addWidget.Verify(a => a.OpenDialog(), Times.Once);
    }
}
