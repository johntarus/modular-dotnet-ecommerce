using BuildingBlocks.Exceptions;
using Catalog.Domain.Events;
using Catalog.Features.Products.CreateProduct;
using Catalog.Features.Products.DeleteProduct;
using Catalog.Features.Products.GetProductById;
using Catalog.Features.Products.GetProducts;
using Catalog.Features.Products.UpdateProduct;
using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests.Features;

public class CreateProductTests(PostgresFixture db) : CatalogTestBase(db)
{
    [Fact]
    public async Task PersistsProduct_AndDispatchesProductCreated()
    {
        var categoryId = await SeedCategoryAsync();

        await using var context = NewContext();
        var id = await new CreateProductCommandHandler(context)
            .Handle(new CreateProductCommand("Mug", "Ceramic", 12.99m, categoryId), CancellationToken.None);

        var stored = (await LoadProductAsync(id))!;
        Assert.Equal("Mug", stored.Name);
        Assert.Equal(12.99m, stored.Price);
        Assert.Equal(categoryId, stored.CategoryId);
        var created = Assert.IsType<ProductCreatedDomainEvent>(Assert.Single(DomainEvents.Published));
        Assert.Equal(id, created.ProductId);
    }

    [Fact]
    public async Task DescriptionAtColumnLimit_IsStored()
    {
        var categoryId = await SeedCategoryAsync();
        var description = new string('d', 1000);

        await using var context = NewContext();
        var id = await new CreateProductCommandHandler(context)
            .Handle(new CreateProductCommand("Mug", description, 1m, categoryId), CancellationToken.None);

        Assert.Equal(description, (await LoadProductAsync(id))!.Description);
    }
}

public class UpdateProductTests(PostgresFixture db) : CatalogTestBase(db)
{
    private async Task UpdateAsync(Guid id, string name, decimal price, Guid categoryId)
    {
        await using var context = NewContext();
        await new UpdateProductCommandHandler(context)
            .Handle(new UpdateProductCommand(name, "Updated", price, categoryId) { Id = id }, CancellationToken.None);
    }

    [Fact]
    public async Task UpdatesFields_AndDispatchesUpdatedAndPriceChanged()
    {
        var categoryId = await SeedCategoryAsync();
        var otherCategory = await SeedCategoryAsync("Garden");
        var id = await SeedProductAsync(categoryId, price: 10m);

        await UpdateAsync(id, "Big mug", 15m, otherCategory);

        var stored = (await LoadProductAsync(id))!;
        Assert.Equal("Big mug", stored.Name);
        Assert.Equal("Updated", stored.Description);
        Assert.Equal(15m, stored.Price);
        Assert.Equal(otherCategory, stored.CategoryId);
        Assert.Contains(DomainEvents.Published, e => e is ProductUpdatedDomainEvent);
        var priceChanged = Assert.Single(DomainEvents.Published.OfType<ProductPriceChangedDomainEvent>());
        Assert.Equal(10m, priceChanged.OldPrice);
        Assert.Equal(15m, priceChanged.NewPrice);
    }

    [Fact]
    public async Task SamePrice_DoesNotDispatchPriceChanged()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, price: 10m);

        await UpdateAsync(id, "Mug", 10m, categoryId);

        Assert.Empty(DomainEvents.Published.OfType<ProductPriceChangedDomainEvent>());
    }

    [Fact]
    public async Task UnknownProduct_ThrowsNotFound()
    {
        var categoryId = await SeedCategoryAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateAsync(Guid.NewGuid(), "Mug", 1m, categoryId));
    }

    [Fact]
    public async Task DeletedProduct_ThrowsNotFound_AndIsUnchanged()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, name: "Old", deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateAsync(id, "New", 1m, categoryId));
        Assert.Equal("Old", (await LoadProductAsync(id))!.Name);
    }
}

public class DeleteProductTests(PostgresFixture db) : CatalogTestBase(db)
{
    private async Task DeleteAsync(Guid id)
    {
        await using var context = NewContext();
        await new DeleteProductCommandHandler(context).Handle(new DeleteProductCommand(id), CancellationToken.None);
    }

    [Fact]
    public async Task SoftDeletes_AndDispatchesProductDeleted()
    {
        var id = await SeedProductAsync(await SeedCategoryAsync());

        await DeleteAsync(id);

        Assert.True((await LoadProductAsync(id))!.IsDeleted);
        Assert.Equal(id, Assert.IsType<ProductDeletedDomainEvent>(Assert.Single(DomainEvents.Published)).ProductId);
    }

    [Fact]
    public async Task UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeletingTwice_ThrowsNotFound_AndDispatchesOnlyOnce()
    {
        var id = await SeedProductAsync(await SeedCategoryAsync());

        await DeleteAsync(id);
        await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(id));

        Assert.Single(DomainEvents.Published.OfType<ProductDeletedDomainEvent>());
    }
}

public class ProductQueryTests(PostgresFixture db) : CatalogTestBase(db)
{
    [Fact]
    public async Task GetById_ReturnsProduct()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, name: "Mug", price: 9.5m);

        await using var context = NewContext();
        var product = await new GetProductByIdQueryHandler(context).Handle(new GetProductByIdQuery(id), CancellationToken.None);

        Assert.Equal("Mug", product.Name);
        Assert.Equal(9.5m, product.Price);
        Assert.Equal(categoryId, product.CategoryId);
    }

    [Fact]
    public async Task GetById_DeletedOrUnknown_ThrowsNotFound()
    {
        var deleted = await SeedProductAsync(await SeedCategoryAsync(), deleted: true);

        await using var context = NewContext();
        var handler = new GetProductByIdQueryHandler(context);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetProductByIdQuery(deleted), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task GetProducts_OrderedByName_PagedAndExcludingDeleted()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Cup");
        await SeedProductAsync(categoryId, name: "Apron");
        await SeedProductAsync(categoryId, name: "Bowl");
        await SeedProductAsync(categoryId, name: "Aardvark", deleted: true);

        await using var context = NewContext();
        var handler = new GetProductsQueryHandler(context);
        var first = await handler.Handle(new GetProductsQuery(1, 2), CancellationToken.None);
        var second = await handler.Handle(new GetProductsQuery(2, 2), CancellationToken.None);

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(["Apron", "Bowl"], first.Items.Select(p => p.Name));
        Assert.Equal(["Cup"], second.Items.Select(p => p.Name));
    }
}
