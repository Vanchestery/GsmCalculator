using GsmCalculator.Models;
using GsmCalculator.Services;
using Moq;
using Xunit;

namespace GsmCalculator.Tests;

/// <summary>
/// Тесты на FavoritesService — управление списком закреплённых виджетов.
/// </summary>
public class FavoritesServiceTests
{
    /// <summary>Фабрика SUT с in-memory моком ISettingsService.</summary>
    private static (FavoritesService sut, AppSettings backing) Create(IEnumerable<Guid>? initial = null)
    {
        var backing = new AppSettings
        {
            FavoriteWidgetIds = initial?.ToList() ?? new List<Guid>()
        };
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Load()).Returns(() => backing);
        settings.Setup(s => s.Save(It.IsAny<AppSettings>()))
                .Callback<AppSettings>(s => backing.FavoriteWidgetIds = new List<Guid>(s.FavoriteWidgetIds));

        return (new FavoritesService(settings.Object), backing);
    }

    [Fact]
    public void Constructor_EmptyFile_GetFavoriteIds_ReturnsEmpty()
    {
        var (sut, _) = Create();
        Assert.Empty(sut.GetFavoriteIds());
    }

    [Fact]
    public void Constructor_LoadsExistingFavorites()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var (sut, _) = Create(new[] { id1, id2 });

        Assert.Equal(new[] { id1, id2 }, sut.GetFavoriteIds());
    }

    [Fact]
    public void Add_NewId_AppendsToEnd_AndPersists()
    {
        var (sut, backing) = Create();
        var id = Guid.NewGuid();

        sut.Add(id);

        Assert.Contains(id, sut.GetFavoriteIds());
        Assert.Contains(id, backing.FavoriteWidgetIds);
    }

    [Fact]
    public void Add_ExistingId_NoOp()
    {
        var id = Guid.NewGuid();
        var (sut, _) = Create(new[] { id });

        sut.Add(id);

        Assert.Single(sut.GetFavoriteIds());
    }

    [Fact]
    public void Remove_ExistingId_RemovesAndPersists()
    {
        var id = Guid.NewGuid();
        var (sut, backing) = Create(new[] { id });

        sut.Remove(id);

        Assert.Empty(sut.GetFavoriteIds());
        Assert.Empty(backing.FavoriteWidgetIds);
    }

    [Fact]
    public void Remove_UnknownId_NoOp()
    {
        var (sut, _) = Create();
        sut.Remove(Guid.NewGuid()); // не бросает
        Assert.Empty(sut.GetFavoriteIds());
    }

    [Fact]
    public void Toggle_FlipsState()
    {
        var (sut, _) = Create();
        var id = Guid.NewGuid();

        sut.Toggle(id);
        Assert.True(sut.IsFavorite(id));

        sut.Toggle(id);
        Assert.False(sut.IsFavorite(id));
    }

    [Fact]
    public void Add_RaisesFavoritesChangedEvent()
    {
        var (sut, _) = Create();
        var raised = 0;
        sut.FavoritesChanged += (_, _) => raised++;

        sut.Add(Guid.NewGuid());

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Add_DuplicateId_DoesNotRaiseEvent()
    {
        var id = Guid.NewGuid();
        var (sut, _) = Create(new[] { id });
        var raised = 0;
        sut.FavoritesChanged += (_, _) => raised++;

        sut.Add(id); // повтор — no-op
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Remove_RaisesFavoritesChangedEvent()
    {
        var id = Guid.NewGuid();
        var (sut, _) = Create(new[] { id });
        var raised = 0;
        sut.FavoritesChanged += (_, _) => raised++;

        sut.Remove(id);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Remove_UnknownId_DoesNotRaiseEvent()
    {
        var (sut, _) = Create();
        var raised = 0;
        sut.FavoritesChanged += (_, _) => raised++;

        sut.Remove(Guid.NewGuid());

        Assert.Equal(0, raised);
    }

    [Fact]
    public void IsFavorite_ReturnsTrueForPinned()
    {
        var id = Guid.NewGuid();
        var (sut, _) = Create(new[] { id });

        Assert.True(sut.IsFavorite(id));
        Assert.False(sut.IsFavorite(Guid.NewGuid()));
    }

    [Fact]
    public void Add_PreservesOrder()
    {
        var (sut, _) = Create();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        sut.Add(a);
        sut.Add(b);
        sut.Add(c);

        Assert.Equal(new[] { a, b, c }, sut.GetFavoriteIds());
    }
}
