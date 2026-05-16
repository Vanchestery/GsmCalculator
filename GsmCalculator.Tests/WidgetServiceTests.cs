using System;
using System.IO;
using System.Linq;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на WidgetService — сеет встроенные виджеты при первом запуске,
/// управляет пользовательскими, защищает встроенные от удаления.
/// </summary>
public class WidgetServiceTests : IDisposable
{
    private readonly string _tempFile;

    public WidgetServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"gsm_widgets_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public void Constructor_FirstRun_SeedsSevenBuiltInWidgets()
    {
        var sut = new WidgetService(_tempFile);

        var all = sut.GetAll();
        Assert.Equal(7, all.Count);
        Assert.All(all, w => Assert.True(w.IsBuiltIn));
    }

    [Fact]
    public void Constructor_FirstRun_ContainsExpectedBuiltIns()
    {
        var sut = new WidgetService(_tempFile);

        var names = sut.GetAll().Select(w => w.Name).ToHashSet();
        Assert.Contains("АИ-92", names);
        Assert.Contains("ДТ-Л", names);
        Assert.Contains("ДТ-З", names);
        Assert.Contains("ТС-1", names);
        Assert.Contains("Масла", names);
        Assert.Contains("ТЖ", names);
        Assert.Contains("ОЖ", names);
    }

    [Fact]
    public void Constructor_FirstRun_CreatesFile()
    {
        _ = new WidgetService(_tempFile);
        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public void Add_AppendsWidget_AndPersists()
    {
        var sut = new WidgetService(_tempFile);
        var custom = new Widget { Name = "Тест", DefaultDensity = 0.9, IsBuiltIn = false };

        sut.Add(custom);

        Assert.Equal(8, sut.GetAll().Count);
        // Перечитываем с диска новым сервисом.
        var reloaded = new WidgetService(_tempFile);
        Assert.Equal(8, reloaded.GetAll().Count);
        Assert.Contains(reloaded.GetAll(), w => w.Name == "Тест");
    }

    [Fact]
    public void Add_EmptyName_Throws()
    {
        var sut = new WidgetService(_tempFile);
        Assert.Throws<ArgumentException>(() =>
            sut.Add(new Widget { Name = "", DefaultDensity = 1.0 }));
    }

    [Fact]
    public void Add_NullWidget_Throws()
    {
        var sut = new WidgetService(_tempFile);
        Assert.Throws<ArgumentNullException>(() => sut.Add(null!));
    }

    [Fact]
    public void Remove_BuiltInWidget_Throws()
    {
        var sut = new WidgetService(_tempFile);
        var builtIn = sut.GetAll().First(w => w.IsBuiltIn);

        Assert.Throws<InvalidOperationException>(() => sut.Remove(builtIn.Id));
    }

    [Fact]
    public void Remove_CustomWidget_RemovesIt()
    {
        var sut = new WidgetService(_tempFile);
        var custom = new Widget { Name = "Удаляемый", DefaultDensity = 1.0, IsBuiltIn = false };
        sut.Add(custom);

        sut.Remove(custom.Id);

        Assert.Equal(7, sut.GetAll().Count);
        Assert.DoesNotContain(sut.GetAll(), w => w.Id == custom.Id);
    }

    [Fact]
    public void Remove_UnknownId_DoesNothing()
    {
        var sut = new WidgetService(_tempFile);
        var before = sut.GetAll().Count;

        sut.Remove(Guid.NewGuid()); // не должно бросить

        Assert.Equal(before, sut.GetAll().Count);
    }

    [Fact]
    public void Find_ExistingId_ReturnsWidget()
    {
        var sut = new WidgetService(_tempFile);
        var first = sut.GetAll().First();

        var found = sut.Find(first.Id);

        Assert.Same(first, found);
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull()
    {
        var sut = new WidgetService(_tempFile);
        Assert.Null(sut.Find(Guid.NewGuid()));
    }
}
