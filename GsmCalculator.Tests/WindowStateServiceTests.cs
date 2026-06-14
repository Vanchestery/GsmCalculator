using System;
using System.IO;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на WindowStateService — отдельный файл window-state.json
/// для запоминания позиции и размера главного окна.
/// </summary>
public class WindowStateServiceTests : IDisposable
{
    private readonly string _tempFile;

    public WindowStateServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"gsm_winstate_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public void Load_NoFile_ReturnsNull()
        => Assert.Null(new WindowStateService(_tempFile).Load());

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var sut = new WindowStateService(_tempFile);
        var state = new MainWindowState
        {
            Left = 100,
            Top = 200,
            Width = 720,
            Height = 560,
            IsMaximized = true
        };

        sut.Save(state);

        var loaded = new WindowStateService(_tempFile).Load();
        Assert.NotNull(loaded);
        Assert.Equal(100, loaded!.Left);
        Assert.Equal(200, loaded.Top);
        Assert.Equal(720, loaded.Width);
        Assert.Equal(560, loaded.Height);
        Assert.True(loaded.IsMaximized);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsNull()
    {
        File.WriteAllText(_tempFile, "not json");
        Assert.Null(new WindowStateService(_tempFile).Load());
    }

    [Fact]
    public void Save_CreatesDirectoryIfMissing()
    {
        var subDir = Path.Combine(Path.GetTempPath(), $"gsm_winstate_subdir_{Guid.NewGuid():N}");
        var nestedPath = Path.Combine(subDir, "window-state.json");
        try
        {
            var sut = new WindowStateService(nestedPath);
            sut.Save(new MainWindowState { Left = 0, Top = 0, Width = 100, Height = 100 });
            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            if (Directory.Exists(subDir)) Directory.Delete(subDir, recursive: true);
        }
    }
}
