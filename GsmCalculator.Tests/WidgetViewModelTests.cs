using GsmCalculator.Models;
using GsmCalculator.Services;
using GsmCalculator.ViewModels;
using Moq;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тестовая реализация <see cref="IDebouncer"/>: запоминает последний
/// callback и выполняет его только при ручном <see cref="Flush"/>.
/// Это даёт детерминированные тесты без работы с реальными таймерами.
/// </summary>
internal class TestDebouncer : IDebouncer
{
    public Action? Pending { get; private set; }
    public int DebounceCallCount { get; private set; }

    public void Debounce(Action callback)
    {
        Pending = callback;
        DebounceCallCount++;
    }

    public void Cancel() => Pending = null;

    public void Flush()
    {
        var p = Pending;
        Pending = null;
        p?.Invoke();
    }
}

/// <summary>
/// Тесты на WidgetViewModel — на v1.2 покрывают:
/// - копирование результата в буфер (Блок G).
/// Тесты конвертации (LitersToKg / KgToLiters) косвенно покрыты через
/// ConversionService unit-тесты + smoke-сценарии в приложении.
/// </summary>
public class WidgetViewModelTests
{
    /// <summary>Контекст одного теста — все управляемые зависимости в одном объекте.</summary>
    private sealed class Sut
    {
        public required WidgetViewModel Vm { get; init; }
        public required Mock<IClipboardService> Clipboard { get; init; }
        public required MainViewModel MainVm { get; init; }
        public required Widget Widget { get; init; }
        public required Mock<IWidgetService> WidgetService { get; init; }
        public required TestDebouncer Debouncer { get; init; }
    }

    private static Sut CreateSut()
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

        var widgetService = new Mock<IWidgetService>();
        // По умолчанию Find возвращает исходный виджет — re-fetch в SaveCurrentState даст актуальный.
        widgetService.Setup(s => s.Find(widget.Id)).Returns(widget);

        var debouncer = new TestDebouncer();

        var favoritesMock = new Mock<IFavoritesService>();
        favoritesMock.Setup(f => f.GetFavoriteIds()).Returns(new List<Guid>());

        var mainVm = new MainViewModel(
            new CalculatorService(), settings.Object,
            Mock.Of<IAddWidgetWindowService>(), Mock.Of<ISettingsWindowService>(),
            loc.Object, Mock.Of<IClipboardService>(),
            widgetService.Object, favoritesMock.Object, Mock.Of<IWidgetWindowService>());

        var vm = new WidgetViewModel(
            widget,
            new ConversionService(),
            new CalculatorService(),
            mainVm,
            loc.Object,
            clipboard.Object,
            widgetService.Object,
            debouncer);

        return new Sut
        {
            Vm = vm,
            Clipboard = clipboard,
            MainVm = mainVm,
            Widget = widget,
            WidgetService = widgetService,
            Debouncer = debouncer
        };
    }

    // ----------------------------------------------------------------
    // Блок G — Копирование в буфер
    // ----------------------------------------------------------------

    [Fact]
    public void CopyResultCommand_BeforeAnyConversion_DoesNothing()
    {
        var sut = CreateSut();

        Assert.False(sut.Vm.CopyResultCommand.CanExecute(null));
        sut.Vm.CopyResultCommand.Execute(null);

        sut.Clipboard.Verify(c => c.SetText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CopyResultCommand_AfterConversion_CopiesRoundedNumberOnly()
    {
        var sut = CreateSut();
        sut.MainVm.SetDisplayValue(100);

        sut.Vm.LitersToKgCommand.Execute(null);

        Assert.True(sut.Vm.CopyResultCommand.CanExecute(null));
        sut.Vm.CopyResultCommand.Execute(null);

        sut.Clipboard.Verify(c => c.SetText("75"), Times.Once);
    }

    [Fact]
    public void CopyResultCommand_RespectsDecimalPlaces()
    {
        var sut = CreateSut();
        sut.Vm.DecimalPlaces = 1;
        sut.MainVm.SetDisplayValue(13);

        sut.Vm.LitersToKgCommand.Execute(null);
        sut.Vm.CopyResultCommand.Execute(null);

        sut.Clipboard.Verify(c => c.SetText("9.8"), Times.Once);
    }

    [Fact]
    public void CopyResultCommand_AfterError_DoesNothing()
    {
        var sut = CreateSut();
        sut.Vm.DensityText = "0";
        sut.MainVm.SetDisplayValue(100);

        sut.Vm.LitersToKgCommand.Execute(null);

        Assert.False(sut.Vm.CopyResultCommand.CanExecute(null));
        sut.Clipboard.Verify(c => c.SetText(It.IsAny<string>()), Times.Never);
    }

    // ----------------------------------------------------------------
    // Блок H — Авто-сохранение изменений виджета
    // ----------------------------------------------------------------

    [Fact]
    public void AutoSave_Constructor_DoesNotTriggerSave()
    {
        // Инициализация полей в конструкторе НЕ должна планировать сохранение —
        // значения только что прочитаны из widget definition.
        var sut = CreateSut();
        Assert.Equal(0, sut.Debouncer.DebounceCallCount);
        sut.WidgetService.Verify(s => s.Update(It.IsAny<Widget>()), Times.Never);
    }

    [Fact]
    public void AutoSave_DensityChange_SchedulesSave()
    {
        var sut = CreateSut();
        sut.Vm.DensityText = "0.85";

        Assert.Equal(1, sut.Debouncer.DebounceCallCount);
        sut.WidgetService.Verify(s => s.Update(It.IsAny<Widget>()), Times.Never); // ещё не flushed
    }

    [Fact]
    public void AutoSave_DecimalPlacesChange_SchedulesSave()
    {
        var sut = CreateSut();
        sut.Vm.DecimalPlaces = 3;

        Assert.Equal(1, sut.Debouncer.DebounceCallCount);
    }

    [Fact]
    public void AutoSave_MultipleChanges_DebounceIntoSingleSave()
    {
        // Несколько быстрых изменений — несколько Debounce-вызовов,
        // но только последний эффективен (TestDebouncer ведёт себя как реальный).
        var sut = CreateSut();
        sut.Vm.DensityText = "0.8";
        sut.Vm.DensityText = "0.85";
        sut.Vm.DensityText = "0.9";

        Assert.Equal(3, sut.Debouncer.DebounceCallCount);

        sut.Debouncer.Flush();

        sut.WidgetService.Verify(
            s => s.Update(It.Is<Widget>(w => w.DefaultDensity == 0.9)),
            Times.Once);
    }

    [Fact]
    public void AutoSave_AfterFlush_PersistsCorrectValues()
    {
        var sut = CreateSut();
        sut.Vm.DensityText = "0.85";
        sut.Vm.DecimalPlaces = 3;
        sut.Debouncer.Flush();

        sut.WidgetService.Verify(
            s => s.Update(It.Is<Widget>(w =>
                w.Id == sut.Widget.Id &&
                w.Name == "Тест" &&
                w.DensityMode == DensityMode.Variable &&
                w.DefaultDensity == 0.85 &&
                w.DefaultDecimalPlaces == 3 &&
                !w.IsBuiltIn)),
            Times.Once);
    }

    [Fact]
    public void AutoSave_BuiltInWidget_PreservesIsBuiltInFlag()
    {
        // Для встроенного виджета — IsBuiltIn в сохранённом объекте должен остаться true.
        // (Сам WidgetService.Update тоже защищает это, но мы передаём корректно.)
        var sut = CreateSut();
        // Подменяем виджет в моке на «встроенный» с тем же Id.
        var builtIn = new Widget
        {
            Id = sut.Widget.Id,
            Name = "Тест",
            DensityMode = DensityMode.Variable,
            DefaultDensity = 0.75,
            DefaultDecimalPlaces = 2,
            IsBuiltIn = true
        };
        sut.WidgetService.Setup(s => s.Find(sut.Widget.Id)).Returns(builtIn);

        sut.Vm.DensityText = "0.85";
        sut.Debouncer.Flush();

        sut.WidgetService.Verify(
            s => s.Update(It.Is<Widget>(w => w.IsBuiltIn)),
            Times.Once);
    }

    [Fact]
    public void AutoSave_DeletedWidget_DoesNotResurrect()
    {
        // Если за время debounce виджет удалили (Find возвращает null) —
        // авто-сохранение должно тихо пропустить, не воскрешать.
        var sut = CreateSut();
        sut.Vm.DensityText = "0.85";
        sut.WidgetService.Setup(s => s.Find(sut.Widget.Id)).Returns((Widget?)null);

        sut.Debouncer.Flush();

        sut.WidgetService.Verify(s => s.Update(It.IsAny<Widget>()), Times.Never);
    }

    [Fact]
    public void AutoSave_ApplyState_DoesNotTriggerSave()
    {
        // Восстановление сессии не должно писать на диск те значения,
        // которые мы только что с него прочитали.
        var sut = CreateSut();
        var debouncesBefore = sut.Debouncer.DebounceCallCount;

        sut.Vm.ApplyState(0.99, 3);

        Assert.Equal(debouncesBefore, sut.Debouncer.DebounceCallCount);
    }

    [Fact]
    public void AutoSave_OnDispose_FlushesPendingSave()
    {
        // Если юзер закрыл окно до истечения debounce — изменение должно сохраниться.
        var sut = CreateSut();
        sut.Vm.DensityText = "0.85";

        sut.Vm.Dispose();

        sut.WidgetService.Verify(
            s => s.Update(It.Is<Widget>(w => w.DefaultDensity == 0.85)),
            Times.Once);
    }

    [Fact]
    public void AutoSave_RefetchesFromService_BeforePersisting()
    {
        // Сценарий: виджет переименовали через Add Widget (E) во время debounce.
        // Авто-сохранение должно прочитать актуальное Name из сервиса, не из _widget.
        var sut = CreateSut();
        sut.Vm.DensityText = "0.85";

        var renamed = new Widget
        {
            Id = sut.Widget.Id,
            Name = "Переименован",      // <-- свежее имя
            DensityMode = sut.Widget.DensityMode,
            DefaultDensity = sut.Widget.DefaultDensity,
            DefaultDecimalPlaces = sut.Widget.DefaultDecimalPlaces,
            IsBuiltIn = sut.Widget.IsBuiltIn
        };
        sut.WidgetService.Setup(s => s.Find(sut.Widget.Id)).Returns(renamed);

        sut.Debouncer.Flush();

        sut.WidgetService.Verify(
            s => s.Update(It.Is<Widget>(w =>
                w.Name == "Переименован" &&
                w.DefaultDensity == 0.85)),
            Times.Once);
    }
}
