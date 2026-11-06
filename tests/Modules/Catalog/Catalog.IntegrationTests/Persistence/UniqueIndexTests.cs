using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Catalog.IntegrationTests.Persistence;

/// <summary>
/// The unique indexes from the UniqueActiveNames migration: the safety net when two requests
/// race past the validators. Rows are saved directly here, bypassing validation.
/// </summary>
public class UniqueIndexTests(PostgresFixture db) : CatalogTestBase(db)
{
    private const string UniqueViolation = "23505";

    private async Task SaveAsync(Action<CatalogDbContext> add)
    {
        await using var context = Db.CreateContext(new NoOpPublisher());
        add(context);
        await context.SaveChangesAsync();
    }

    private async Task AssertUniqueViolationAsync(Action<CatalogDbContext> add)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(add));
        Assert.Equal(UniqueViolation, Assert.IsType<PostgresException>(ex.InnerException).SqlState);
    }

    [Theory]
    [InlineData("Kitchen")]
    [InlineData("KITCHEN")]
    public async Task Category_DuplicateActiveName_IgnoringCase_IsRejected(string name)
    {
        await SeedCategoryAsync("Kitchen");

        await AssertUniqueViolationAsync(c => c.Categories.Add(Category.Create(name, null)));
    }

    [Fact]
    public async Task Category_RowSavedWithSpacesBeforeTrimming_StillBlocksTheTrimmedName()
    {
        // Rows written before names were trimmed may still contain spaces; the index trims too.
        await using (var context = Db.CreateContext(new NoOpPublisher()))
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO catalog.categories (\"Id\", \"Name\", \"Description\", \"IsDeleted\", \"CreatedAt\", \"UpdatedAt\") " +
                "VALUES ({0}, '  Kitchen ', NULL, false, now(), now())",
                Guid.NewGuid());

        await AssertUniqueViolationAsync(c => c.Categories.Add(Category.Create("kitchen", null)));
    }

    [Fact]
    public async Task Category_NameOfDeletedCategory_IsAllowed()
    {
        await SeedCategoryAsync("Kitchen", deleted: true);

        await SaveAsync(c => c.Categories.Add(Category.Create("Kitchen", null)));
    }

    [Fact]
    public async Task Product_DuplicateNameInSameCategory_IsRejected()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Mug");

        await AssertUniqueViolationAsync(c => c.Products.Add(Product.Create("mug", "d", 1m, categoryId)));
    }

    [Fact]
    public async Task Product_SameNameInDifferentCategory_IsAllowed()
    {
        await SeedProductAsync(await SeedCategoryAsync("Kitchen"), name: "Mug");
        var garden = await SeedCategoryAsync("Garden");

        await SaveAsync(c => c.Products.Add(Product.Create("Mug", "d", 1m, garden)));
    }

    [Fact]
    public async Task Product_NameOfDeletedProduct_IsAllowed()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Mug", deleted: true);

        await SaveAsync(c => c.Products.Add(Product.Create("Mug", "d", 1m, categoryId)));
    }
}
