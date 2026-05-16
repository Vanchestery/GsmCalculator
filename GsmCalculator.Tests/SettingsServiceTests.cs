using System;
using System.IO;
using GsmCalculator.Models;
using GsmCalculator.Services;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на SettingsService. Файловые операции — на временном файле,
/// уникальном для каждого теста. Очистка в Dispose.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _tempFile;

    public SettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"gsm_settings_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public void Load_FileMissing_ReturnsDefaults()
    {
        var sut = new SettingsService(_tempFile);

        var settings = sut.Load();

        Assert.NotNull(settings);
        Assert.Equal(10, settings.HistorySize);
        Assert.Equal(ColorTheme.Dark, settings.Theme);
        Assert.Equal(AppLanguage.Russian, settings.Language);
        Assert.Equal(CalculatorMode.Classic, settings.CalculatorMode);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        File.WriteAllText(_tempFile, "{ this is not valid json !!!");
        var sut = new SettingsService(_tempFile);

        var settings = sut.Load();

        Assert.NotNull(settings);
        Assert.Equal(10, settings.HistorySize);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var sut = new SettingsService(_tempFile);
        var original = new AppSettings
        {
            HistorySize = 25,
            Theme = ColorTheme.Blue,
            StartupBehavior = StartupBehavior.AlwaysContinue,
            Language = AppLanguage.English,
            CalculatorMode = CalculatorMode.Engineering
        };

        sut.Save(original);

        // Создаём новый сервис чтобы убедиться что прочитали с диска, а не из памяти.
        var loaded = new SettingsService(_tempFile).Load();
        Assert.Equal(25, loaded.HistorySize);
        Assert.Equal(ColorTheme.Blue, loaded.Theme);
        Assert.Equal(StartupBehavior.AlwaysContinue, loaded.StartupBehavior);
        Assert.Equal(AppLanguage.English, loaded.Language);
        Assert.Equal(CalculatorMode.Engineering, loaded.CalculatorMode);
    }

    [Fact]
    public void Save_CreatesFile()
    {
        var sut = new SettingsService(_tempFile);

        sut.Save(AppSettings.CreateDefault());

        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public void Save_CreatesDirectoryIfMissing()
    {
        // Путь с несуществующей подпапкой.
        var subDir = Path.Combine(Path.GetTempPath(), $"gsm_test_subdir_{Guid.NewGuid():N}");
        var nestedPath = Path.Combine(subDir, "settings.json");
        try
        {
            var sut = new SettingsService(nestedPath);
            sut.Save(AppSettings.CreateDefault());
            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            if (Directory.Exists(subDir)) Directory.Delete(subDir, recursive: true);
        }
    }
}
