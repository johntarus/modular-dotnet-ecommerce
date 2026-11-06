using BuildingBlocks.Exceptions;
using Catalog.Features.Categories.CreateCategory;
using Catalog.Features.Categories.DeleteCategory;
using Catalog.Features.Categories.GetCategories;
using Catalog.Features.Categories.GetCategoryById;
using Catalog.Features.Categories.UpdateCategory;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Features;

public class CategoryHandlerTests(PostgresFixture db) : CatalogTestBase(db)
{
    [Fact]
    public async Task Create_PersistsCategory()
    {
        await using var context = NewContext();
        var id = await new CreateCategoryCommandHandler(context)
            .Handle(new CreateCategoryCommand("Kitchen", "Pots"), CancellationToken.None);

        var stored = (await LoadCategoryAsync(id))!;
        Assert.Equal("Kitchen", stored.Name);
        Assert.Equal("Pots", stored.Description);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var id = await SeedCategoryAsync("Kitchen");

        await using var context = NewContext();
        await new UpdateCategoryCommandHandler(context)
            .Handle(new UpdateCategoryCommand(id, "Home", "All of it"), CancellationToken.None);

        var stored = (await LoadCategoryAsync(id))!;
        Assert.Equal("Home", stored.Name);
        Assert.Equal("All of it", stored.Description);
    }

    [Fact]
    public async Task Update_UnknownOrDeleted_ThrowsNotFound()
    {
        var deleted = await SeedCategoryAsync(deleted: true);

        await using var context = NewContext();
        var handler = new UpdateCategoryCommandHandler(context);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateCategoryCommand(Guid.NewGuid(), "x", null), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateCategoryCommand(deleted, "x", null), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_SoftDeletes()
    {
        var id = await SeedCategoryAsync();

        await using var context = NewContext();
        await new DeleteCategoryCommandHandler(context).Handle(new DeleteCategoryCommand(id), CancellationToken.None);

        Assert.True((await LoadCategoryAsync(id))!.IsDeleted);
    }

    [Fact]
    public async Task Delete_WithActiveProducts_ThrowsConflict_AndKeepsCategory()
    {
        var id = await SeedCategoryAsync();
        await SeedProductAsync(id);

        await using var context = NewContext();
        await Assert.ThrowsAsync<ConflictException>(() =>
            new DeleteCategoryCommandHandler(context).Handle(new DeleteCategoryCommand(id), CancellationToken.None));

        Assert.False((await LoadCategoryAsync(id))!.IsDeleted);
    }

    [Fact]
    public async Task Delete_WithOnlyDeletedProducts_Succeeds()
    {
        var id = await SeedCategoryAsync();
        await SeedProductAsync(id, deleted: true);

        await using var context = NewContext();
        await new DeleteCategoryCommandHandler(context).Handle(new DeleteCategoryCommand(id), CancellationToken.None);

        Assert.True((await LoadCategoryAsync(id))!.IsDeleted);
    }

    [Fact]
    public async Task Delete_UnknownOrAlreadyDeleted_ThrowsNotFound()
    {
        var deleted = await SeedCategoryAsync(deleted: true);

        await using var context = NewContext();
        var handler = new DeleteCategoryCommandHandler(context);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteCategoryCommand(deleted), CancellationToken.None));
    }

    [Fact]
    public async Task GetById_ReturnsCategory_DeletedThrowsNotFound()
    {
        var id = await SeedCategoryAsync("Kitchen");
        var deleted = await SeedCategoryAsync("Gone", deleted: true);

        await using var context = NewContext();
        var handler = new GetCategoryByIdQueryHandler(context);

        Assert.Equal("Kitchen", (await handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None)).Name);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetCategoryByIdQuery(deleted), CancellationToken.None));
    }

    [Fact]
    public async Task GetCategories_OrderedByName_ExcludingDeleted()
    {
        await SeedCategoryAsync("Toys");
        await SeedCategoryAsync("Garden");
        await SeedCategoryAsync("Apparel", deleted: true);

        await using var context = NewContext();
        var result = await new GetCategoriesCommandHandler(context).Handle(new GetCategoriesQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Garden", "Toys"], result.Items.Select(c => c.Name));
    }
}
