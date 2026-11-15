using Catalog.Domain.Entities;
using Catalog.IntegrationTests.Infrastructure;
using Pgvector;

namespace Catalog.IntegrationTests.Features.Ai;

public abstract class AiTestBase(PostgresFixture db) : CatalogTestBase(db)
{
    protected async Task<Guid> SeedEmbeddedProductAsync(
        Guid categoryId, string name, float[] embedding, decimal price = 10m, string description = "Description", bool deleted = false)
    {
        var product = Product.Create(name, description, price, categoryId);
        product.SetEmbedding(new Vector(embedding));
        if (deleted) product.Delete();
        await using var context = Db.CreateContext(new NoOpPublisher());
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }
}
