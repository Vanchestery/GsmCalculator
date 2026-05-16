using System;
using System.IO;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на SessionService.
/// </summary>
public class SessionServiceTests : IDisposable
{
    private readonly string _tempFile;

    public SessionServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"gsm_session_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public void HasSavedSession_NoFile_ReturnsFalse()
        => Assert.False(new SessionService(_tempFile).HasSavedSession);

    [Fact]
    public void Load_NoFile_ReturnsNull()
        => Assert.Null(new SessionService(_tempFile).Load());

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var sut = new SessionService(_tempFile);
        var widgetId = Guid.NewGuid();
        var state = new SessionState
        {
            CurrentDisplay = "1250",
            History = { HistoryEntry.Create("5 + 3", "8") },
            OpenWidgets =
            {
                new OpenWidgetState
                {
                    WidgetId = widgetId,
                    Left = 100,
                    Top = 200,
                    CurrentDensity = 0.84,
                    CurrentDecimalPlaces = 2
                }
            }
        };

        sut.Save(state);

        var loaded = new SessionService(_tempFile).Load();
        Assert.NotNull(loaded);
        Assert.Equal("1250", loaded!.CurrentDisplay);
        Assert.Single(loaded.History);
        Assert.Equal("5 + 3", loaded.History[0].Expression);
        Assert.Equal("8", loaded.History[0].Result);
        Assert.Single(loaded.OpenWidgets);
        Assert.Equal(widgetId, loaded.OpenWidgets[0].WidgetId);
        Assert.Equal(100, loaded.OpenWidgets[0].Left);
        Assert.Equal(0.84, loaded.OpenWidgets[0].CurrentDensity);
        Assert.Equal(2, loaded.OpenWidgets[0].CurrentDecimalPlaces);
    }

    [Fact]
    public void HasSavedSession_AfterSave_ReturnsTrue()
    {
        var sut = new SessionService(_tempFile);
        sut.Save(SessionState.CreateEmpty());

        Assert.True(sut.HasSavedSession);
    }

    [Fact]
    public void Clear_RemovesFile()
    {
        var sut = new SessionService(_tempFile);
        sut.Save(SessionState.CreateEmpty());

        sut.Clear();

        Assert.False(sut.HasSavedSession);
        Assert.Null(sut.Load());
    }

    [Fact]
    public void Clear_NoFile_DoesNotThrow()
    {
        var sut = new SessionService(_tempFile);
        sut.Clear(); // не должно бросить даже если файла нет
    }

    [Fact]
    public void Load_CorruptFile_ReturnsNull()
    {
        File.WriteAllText(_tempFile, "not json");
        Assert.Null(new SessionService(_tempFile).Load());
    }

    [Fact]
    public void Save_SetsSavedAtToNow()
    {
        var sut = new SessionService(_tempFile);
        var before = DateTime.Now.AddSeconds(-1);

        sut.Save(SessionState.CreateEmpty());

        var loaded = sut.Load();
        Assert.NotNull(loaded);
        Assert.True(loaded!.SavedAt >= before);
        Assert.True(loaded.SavedAt <= DateTime.Now.AddSeconds(1));
    }
}
