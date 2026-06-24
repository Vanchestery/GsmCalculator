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
        CalculatorMode mode = CalculatorMode.Classic,
        RoundingMode rounding = RoundingMode.None)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(new AppSettings
        {
            HistorySize = historySize,
            CalculatorMode = mode,
            RoundingMode = rounding
        });

        var loc = new Mock<ILocalizationService>();
        loc.Setup(l => l.Get("Common_Error")).Returns("Ошибка");

        return new MainViewModel(
            new CalculatorService(),                  // настоящий — чистая логика
            settings.Object,
            Mock.Of<IAddWidgetWindowService>(),
            Mock.Of<ISettingsWindowService>(),
            loc.Object,
            Mock.Of<IClipboardService>());
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

    // Блок F (v1.2): Negate команда удалена — кнопка '±' заменена на '%'.
    // Тесты на унарный минус больше не актуальны.

    // ----------------------------------------------------------------
    // Блок F — Контекстная команда процентов (Windows Calc Standard style)
    // ----------------------------------------------------------------

    [Fact]
    public void Percent_Classic_Plus_AddsPercentOfLeftOperand()
    {
        // 100 + 10% = 100 + (100 × 0.1) = 110
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        Assert.Equal("10", vm.Display); // 10% от 100 = 10
        vm.EqualsCommand.Execute(null);
        Assert.Equal("110", vm.Display);
    }

    [Fact]
    public void Percent_Classic_Minus_SubtractsPercentOfLeftOperand()
    {
        // 100 − 10% = 100 − (100 × 0.1) = 90
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "100");
        vm.OperationCommand.Execute("-");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        Assert.Equal("10", vm.Display);
        vm.EqualsCommand.Execute(null);
        Assert.Equal("90", vm.Display);
    }

    [Fact]
    public void Percent_Classic_Multiply_DividesBy100()
    {
        // 100 × 10% = 100 × 0.1 = 10
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "100");
        vm.OperationCommand.Execute("×");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        Assert.Equal("0.1", vm.Display);
        vm.EqualsCommand.Execute(null);
        Assert.Equal("10", vm.Display);
    }

    [Fact]
    public void Percent_Classic_Divide_DividesBy100()
    {
        // 100 ÷ 10% = 100 ÷ 0.1 = 1000
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "100");
        vm.OperationCommand.Execute("÷");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        Assert.Equal("0.1", vm.Display);
        vm.EqualsCommand.Execute(null);
        Assert.Equal("1000", vm.Display);
    }

    [Fact]
    public void Percent_Classic_NoOperator_DividesBy100()
    {
        // 10% без оператора = 10 / 100 = 0.1
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        Assert.Equal("0.1", vm.Display);
    }

    [Fact]
    public void Percent_OnZero_GivesZero()
    {
        var vm = CreateSut();
        vm.PercentCommand.Execute(null);
        Assert.Equal("0", vm.Display);
    }

    [Fact]
    public void Percent_Classic_ChainAfterPercent_AutoEvaluates()
    {
        // 100 + 10% + 5 = 115 (matches Windows Calc Standard)
        // Если % не закрывает чейн — было бы 10+5=15. Это контролирует _justResolvedPercent.
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        vm.OperationCommand.Execute("+");
        Enter(vm, "5");
        vm.EqualsCommand.Execute(null);
        Assert.Equal("115", vm.Display);
    }

    [Fact]
    public void Percent_DigitAfter_ReplacesDisplay()
    {
        // После % следующая цифра должна ЗАМЕНИТЬ дисплей, не дописать.
        var vm = CreateSut();
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null); // display="10"
        Enter(vm, "7"); // должен заменить
        Assert.Equal("7", vm.Display);
    }

    [Fact]
    public void Percent_Engineering_AlwaysDividesBy100()
    {
        // В Engineering '%' = просто value/100 (как Windows Calc Scientific).
        // 2 + 3 × 4% = 2 + 3 × 0.04 = 2.12
        var vm = CreateSut(mode: CalculatorMode.Engineering);
        Enter(vm, "2");
        vm.OperationCommand.Execute("+");
        Enter(vm, "3");
        vm.OperationCommand.Execute("×");
        Enter(vm, "4");
        vm.PercentCommand.Execute(null);
        Assert.Equal("0.04", vm.Display);
        vm.EqualsCommand.Execute(null);
        Assert.Equal("2.12", vm.Display);
    }

    [Fact]
    public void Percent_Engineering_ChainAfterPercent_PreservesPercentValue()
    {
        // 2 × 4% + 100 = (2 × 0.04) + 100 = 0.08 + 100 = 100.08
        // Регрессионный тест: % не должен теряться при последующем операторе.
        var vm = CreateSut(mode: CalculatorMode.Engineering);
        Enter(vm, "2");
        vm.OperationCommand.Execute("×");
        Enter(vm, "4");
        vm.PercentCommand.Execute(null);
        vm.OperationCommand.Execute("+");
        Enter(vm, "100");
        vm.EqualsCommand.Execute(null);
        Assert.Equal("100.08", vm.Display);
    }

    [Fact]
    public void Percent_InErrorState_DoesNothing()
    {
        var vm = CreateSut();
        Enter(vm, "5");
        vm.OperationCommand.Execute("÷");
        Enter(vm, "0");
        vm.EqualsCommand.Execute(null);
        Assert.Equal("Ошибка", vm.Display);

        vm.PercentCommand.Execute(null);
        Assert.Equal("Ошибка", vm.Display);
    }

    [Fact]
    public void Percent_HistoryRecorded_OnEquals()
    {
        // История пишется внутри Apply на = — должна показать «100 + 10 = 110».
        var vm = CreateSut(mode: CalculatorMode.Classic);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "10");
        vm.PercentCommand.Execute(null);
        vm.EqualsCommand.Execute(null);

        Assert.Single(vm.History);
        Assert.Equal("100 + 10", vm.History[0].Expression);
        Assert.Equal("110", vm.History[0].Result);
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
            Mock.Of<IAddWidgetWindowService>(), Mock.Of<ISettingsWindowService>(), loc.Object,
            Mock.Of<IClipboardService>());

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
            Mock.Of<IAddWidgetWindowService>(), settingsWindow.Object, loc.Object,
            Mock.Of<IClipboardService>());

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
            addWidget.Object, Mock.Of<ISettingsWindowService>(), loc.Object,
            Mock.Of<IClipboardService>());

        vm.OpenAddWidgetCommand.Execute(null);

        addWidget.Verify(a => a.OpenDialog(), Times.Once);
    }

    // ----------------------------------------------------------------
    // Блок K — Режим округления
    // ----------------------------------------------------------------

    [Fact]
    public void RoundingMode_DefaultsToNone_FromSettings()
    {
        var vm = CreateSut(); // rounding=None по умолчанию
        Assert.Equal(RoundingMode.None, vm.RoundingMode);
    }

    [Fact]
    public void RoundingMode_InitializedFromSettings()
    {
        var vm = CreateSut(rounding: RoundingMode.Integer);
        Assert.Equal(RoundingMode.Integer, vm.RoundingMode);
    }

    [Fact]
    public void CycleRoundingModeCommand_CyclesNoneIntegerOneTenthBack()
    {
        var vm = CreateSut(rounding: RoundingMode.None);

        vm.CycleRoundingModeCommand.Execute(null);
        Assert.Equal(RoundingMode.Integer, vm.RoundingMode);

        vm.CycleRoundingModeCommand.Execute(null);
        Assert.Equal(RoundingMode.OneTenth, vm.RoundingMode);

        vm.CycleRoundingModeCommand.Execute(null);
        Assert.Equal(RoundingMode.None, vm.RoundingMode);
    }

    [Fact]
    public void CycleRoundingMode_PersistsToSettings()
    {
        // Готовим мок настроек явно — нужен Verify на Save.
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(new AppSettings { RoundingMode = RoundingMode.None });
        AppSettings? saved = null;
        settings.Setup(s => s.Save(It.IsAny<AppSettings>()))
                .Callback<AppSettings>(s => saved = s);

        var loc = new Mock<ILocalizationService>();
        var vm = new MainViewModel(
            new CalculatorService(), settings.Object,
            Mock.Of<IAddWidgetWindowService>(), Mock.Of<ISettingsWindowService>(), loc.Object,
            Mock.Of<IClipboardService>());

        vm.CycleRoundingModeCommand.Execute(null);

        Assert.NotNull(saved);
        Assert.Equal(RoundingMode.Integer, saved!.RoundingMode);
    }

    // -- K.5 = Y: округление на каждом фиксировании результата (Classic) --

    [Fact]
    public void Classic_EqualsRoundsResult_Integer()
    {
        var vm = CreateSut(rounding: RoundingMode.Integer);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "7");
        vm.EqualsCommand.Execute(null);

        Assert.Equal("151", vm.Display);
    }

    [Fact]
    public void Classic_EqualsRoundsResult_OneTenth()
    {
        var vm = CreateSut(rounding: RoundingMode.OneTenth);
        // 100 + 50.77 = 150.77 → 150.8 при OneTenth.
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "77");
        vm.EqualsCommand.Execute(null);

        Assert.Equal("150.8", vm.Display);
    }

    [Fact]
    public void Classic_ChainAfterEquals_UsesRoundedAccumulator()
    {
        // K.5=Y: после = аккумулятор становится округлённым числом.
        // Следующая операция строится от него.
        var vm = CreateSut(rounding: RoundingMode.Integer);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "7");
        vm.EqualsCommand.Execute(null);
        // Сейчас display="151", accumulator=151.
        vm.OperationCommand.Execute("+");
        Enter(vm, "0");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "4");
        vm.EqualsCommand.Execute(null);
        // 151 + 0.4 = 151.4 → 151 при Integer.
        Assert.Equal("151", vm.Display);
    }

    [Fact]
    public void Classic_OperatorMidExpression_RoundsIntermediate()
    {
        // Нажатие нового оператора фиксирует предыдущий результат — он тоже округляется.
        var vm = CreateSut(rounding: RoundingMode.Integer);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "7");
        vm.OperationCommand.Execute("+"); // фиксирует 100+50.7=150.7→151
        Enter(vm, "10");
        vm.EqualsCommand.Execute(null);
        // 151 + 10 = 161
        Assert.Equal("161", vm.Display);
    }

    // -- K.5 = Y: округление на = в Engineering --

    [Fact]
    public void Engineering_EqualsRoundsResult()
    {
        var vm = CreateSut(mode: CalculatorMode.Engineering, rounding: RoundingMode.Integer);
        // 2 + 3 × 4.7 = 16.1 → 16 при Integer.
        Enter(vm, "2");
        vm.OperationCommand.Execute("+");
        Enter(vm, "3");
        vm.OperationCommand.Execute("×");
        Enter(vm, "4");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "7");
        vm.EqualsCommand.Execute(null);
        Assert.Equal("16", vm.Display);
    }

    // -- Округление в режиме None — точные значения --

    [Fact]
    public void Classic_NoneMode_PreservesPrecision()
    {
        var vm = CreateSut(rounding: RoundingMode.None);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "7");
        vm.EqualsCommand.Execute(null);
        Assert.Equal("150.7", vm.Display);
    }

    // -- SetDisplayValue (виджет) применяет округление --

    [Fact]
    public void SetDisplayValue_AppliesRounding_Integer()
    {
        var vm = CreateSut(rounding: RoundingMode.Integer);
        // Виджет отдал «точное» 73.7 — должно прилететь на дисплей как 74.
        vm.SetDisplayValue(73.7);
        Assert.Equal("74", vm.Display);
    }

    [Fact]
    public void SetDisplayValue_NoneMode_PreservesValue()
    {
        var vm = CreateSut(rounding: RoundingMode.None);
        vm.SetDisplayValue(73.7);
        Assert.Equal("73.7", vm.Display);
    }

    [Fact]
    public void RoundingIndicator_ReflectsCurrentMode()
    {
        var vm = CreateSut(rounding: RoundingMode.None);
        Assert.Equal("∞", vm.RoundingIndicator);
        vm.CycleRoundingModeCommand.Execute(null);
        Assert.Equal("1", vm.RoundingIndicator);
        vm.CycleRoundingModeCommand.Execute(null);
        Assert.Equal("0.1", vm.RoundingIndicator);
    }

    // ----------------------------------------------------------------
    // Блок G — Копирование в буфер
    // ----------------------------------------------------------------

    /// <summary>Фабрика VM с моком буфера для verify.</summary>
    private static (MainViewModel vm, Mock<IClipboardService> clipboard) CreateSutWithClipboardMock(
        RoundingMode rounding = RoundingMode.None)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(new AppSettings { RoundingMode = rounding });
        var loc = new Mock<ILocalizationService>();
        loc.Setup(l => l.Get("Common_Error")).Returns("Ошибка");
        var clipboard = new Mock<IClipboardService>();

        var vm = new MainViewModel(
            new CalculatorService(), settings.Object,
            Mock.Of<IAddWidgetWindowService>(), Mock.Of<ISettingsWindowService>(),
            loc.Object, clipboard.Object);

        return (vm, clipboard);
    }

    [Fact]
    public void CopyDisplay_NoCalculations_CopiesZero()
    {
        var (vm, clipboard) = CreateSutWithClipboardMock();
        vm.CopyDisplayCommand.Execute(null);
        clipboard.Verify(c => c.SetText("0"), Times.Once);
    }

    [Fact]
    public void CopyDisplay_AfterDigits_CopiesDisplayString()
    {
        var (vm, clipboard) = CreateSutWithClipboardMock();
        Enter(vm, "1250");
        vm.CopyDisplayCommand.Execute(null);
        clipboard.Verify(c => c.SetText("1250"), Times.Once);
    }

    [Fact]
    public void CopyDisplay_AfterEquals_CopiesResult()
    {
        var (vm, clipboard) = CreateSutWithClipboardMock();
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.EqualsCommand.Execute(null);

        vm.CopyDisplayCommand.Execute(null);

        clipboard.Verify(c => c.SetText("150"), Times.Once);
    }

    [Fact]
    public void CopyDisplay_RespectsRounding_CopiesRoundedValue()
    {
        // По G + K: в буфер идёт то что юзер видит — округлённое.
        var (vm, clipboard) = CreateSutWithClipboardMock(rounding: RoundingMode.Integer);
        Enter(vm, "100");
        vm.OperationCommand.Execute("+");
        Enter(vm, "50");
        vm.DecimalCommand.Execute(null);
        Enter(vm, "7");
        vm.EqualsCommand.Execute(null);
        // Display = "151" (округлённое из 150.7)

        vm.CopyDisplayCommand.Execute(null);
        clipboard.Verify(c => c.SetText("151"), Times.Once);
    }

    [Fact]
    public void CopyDisplay_InErrorState_DoesNotCopy()
    {
        var (vm, clipboard) = CreateSutWithClipboardMock();
        Enter(vm, "5");
        vm.OperationCommand.Execute("÷");
        Enter(vm, "0");
        vm.EqualsCommand.Execute(null);
        Assert.Equal("Ошибка", vm.Display);

        // CanExecute должен быть false → команда не дёргает clipboard.
        Assert.False(vm.CopyDisplayCommand.CanExecute(null));
        vm.CopyDisplayCommand.Execute(null);

        clipboard.Verify(c => c.SetText(It.IsAny<string>()), Times.Never);
    }
}
