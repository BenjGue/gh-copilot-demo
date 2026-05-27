using albums_api.Controllers;
using albums_api.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace albums_api.Tests;

public class AlbumModelTests
{
    [Fact]
    public void GetAll_ReturnsSeededAlbums()
    {
        var all = Album.GetAll();
        Assert.NotEmpty(all);
        Assert.All(all, a => Assert.False(string.IsNullOrWhiteSpace(a.Title)));
    }

    [Fact]
    public void GetById_ReturnsAlbum_WhenExists()
    {
        var album = Album.GetById(1);
        Assert.NotNull(album);
        Assert.Equal(1, album!.Id);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenMissing()
    {
        Assert.Null(Album.GetById(int.MaxValue));
    }

    [Fact]
    public void GetByYear_FiltersByYear()
    {
        var result = Album.GetByYear(2022);
        Assert.NotEmpty(result);
        Assert.All(result, a => Assert.Equal(2022, a.Year));
    }

    [Fact]
    public void Add_AssignsNextId_AndPersists()
    {
        var artist = new Artist("Test Artist", new DateOnly(2000, 1, 1), "Testville");
        var input = new Album(0, "Test Title", artist, 1.23, "https://example.com/img.png", 2025);

        var created = Album.Add(input);

        Assert.True(created.Id > 0);
        Assert.Equal("Test Title", created.Title);
        Assert.Equal(artist, created.Artist);
        Assert.NotNull(Album.GetById(created.Id));

        Album.Delete(created.Id);
    }

    [Fact]
    public void Update_ModifiesExistingAlbum()
    {
        var artist = new Artist("Original", new DateOnly(1990, 5, 5), "Origin City");
        var created = Album.Add(new Album(0, "Original Title", artist, 9.99, "https://example.com/o.png", 2024));

        var newArtist = new Artist("Updated", new DateOnly(1991, 6, 6), "New City");
        var updated = Album.Update(created.Id, new Album(0, "Updated Title", newArtist, 19.99, "https://example.com/u.png", 2025));

        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal(newArtist, updated.Artist);

        Album.Delete(created.Id);
    }

    [Fact]
    public void Update_ReturnsNull_WhenMissing()
    {
        var artist = new Artist("X", new DateOnly(2000, 1, 1), "Nowhere");
        var result = Album.Update(int.MaxValue, new Album(0, "X", artist, 0, "", 2025));
        Assert.Null(result);
    }

    [Fact]
    public void Delete_RemovesAlbum()
    {
        var artist = new Artist("ToDelete", new DateOnly(2000, 1, 1), "Gone");
        var created = Album.Add(new Album(0, "Bye", artist, 1, "", 2025));

        Assert.True(Album.Delete(created.Id));
        Assert.Null(Album.GetById(created.Id));
        Assert.False(Album.Delete(created.Id));
    }
}

public class AlbumControllerTests
{
    private static Album SampleAlbum(string title = "Ctrl Album") =>
        new(0, title, new Artist("Ctrl Artist", new DateOnly(1995, 3, 14), "Ctrl City"), 11.11, "https://example.com/c.png", 2025);

    [Fact]
    public void Get_ReturnsOk_WithAlbums()
    {
        var controller = new AlbumController();
        var result = controller.Get() as OkObjectResult;

        Assert.NotNull(result);
        var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result!.Value);
        Assert.NotEmpty(albums);
    }

    [Fact]
    public void GetById_ReturnsOk_WhenFound()
    {
        var controller = new AlbumController();
        var result = controller.Get(1);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Album>(ok.Value);
    }

    [Fact]
    public void GetById_ReturnsNotFound_WhenMissing()
    {
        var controller = new AlbumController();
        var result = controller.Get(int.MaxValue);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetByYear_ReturnsOk_WithFilteredAlbums()
    {
        var controller = new AlbumController();
        var result = controller.GetByYear(2023) as OkObjectResult;

        Assert.NotNull(result);
        var albums = Assert.IsAssignableFrom<IEnumerable<Album>>(result!.Value);
        Assert.All(albums, a => Assert.Equal(2023, a.Year));
    }

    [Fact]
    public void Create_ReturnsCreatedAtAction_WithNewId()
    {
        var controller = new AlbumController();
        var result = controller.Create(SampleAlbum()) as CreatedAtActionResult;

        Assert.NotNull(result);
        var created = Assert.IsType<Album>(result!.Value);
        Assert.True(created.Id > 0);

        Album.Delete(created.Id);
    }

    [Fact]
    public void Update_ReturnsOk_WhenFound()
    {
        var controller = new AlbumController();
        var created = Album.Add(SampleAlbum("To Update"));

        var result = controller.Update(created.Id, SampleAlbum("Updated")) as OkObjectResult;

        Assert.NotNull(result);
        var updated = Assert.IsType<Album>(result!.Value);
        Assert.Equal("Updated", updated.Title);

        Album.Delete(created.Id);
    }

    [Fact]
    public void Update_ReturnsNotFound_WhenMissing()
    {
        var controller = new AlbumController();
        var result = controller.Update(int.MaxValue, SampleAlbum());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Delete_ReturnsNoContent_WhenFound()
    {
        var controller = new AlbumController();
        var created = Album.Add(SampleAlbum("To Delete"));

        var result = controller.Delete(created.Id);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Delete_ReturnsNotFound_WhenMissing()
    {
        var controller = new AlbumController();
        var result = controller.Delete(int.MaxValue);
        Assert.IsType<NotFoundResult>(result);
    }
}
