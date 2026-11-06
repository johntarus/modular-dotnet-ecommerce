using Catalog.Infrastructure;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Features;

/// <summary>ICatalogApi is what Cart uses to look up products and their current price.</summary>
public class CatalogApiTests(PostgresFixture db) : CatalogTestBase(db)
{
    [Fact]
    public async Task GetProductById_ReturnsCurrentData()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, name: "Mug", price: 19.99m);

        await using var context = NewContext();
        var dto = await new CatalogApi(context).GetProductByIdAsync(id);

        Assert.NotNull(dto);
        Assert.Equal("Mug", dto.Name);
        Assert.Equal(19.99m, dto.Price);
        Assert.Equal(categoryId, dto.CategoryId);
    }

    [Fact]
    public async Task DeletedOrUnknownProduct_IsNull_AndDoesNotExist()
    {
        var deleted = await SeedProductAsync(await SeedCategoryAsync(), deleted: true);

        await using var context = NewContext();
        var api = new CatalogApi(context);

        Assert.Null(await api.GetProductByIdAsync(deleted));
        Assert.Null(await api.GetProductByIdAsync(Guid.NewGuid()));
        Assert.False(await api.ProductExistsAsync(deleted));
        Assert.False(await api.ProductExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ProductExists_ForActiveProduct()
    {
        var id = await SeedProductAsync(await SeedCategoryAsync());

        await using var context = NewContext();
        Assert.True(await new CatalogApi(context).ProductExistsAsync(id));
    }
}
