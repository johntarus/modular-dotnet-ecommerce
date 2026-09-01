using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=modular_monolith;Username=postgres;Password=postgres");
        return new CatalogDbContext(optionBuilder.Options);
    }
}