using System.Diagnostics;

namespace BuildingBlocks.Infrastructure.Observability;

public class ApplicationActivitySource
{
    public static readonly ActivitySource Instance =
        new("Ecommerce");
}