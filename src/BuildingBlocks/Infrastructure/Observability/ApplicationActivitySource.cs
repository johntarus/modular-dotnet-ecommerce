using System.Diagnostics;

namespace BuildingBlocks.Infrastructure.Observability;

public static class ApplicationActivitySource
{
    public static readonly ActivitySource Instance =
        new("Ecommerce");
}