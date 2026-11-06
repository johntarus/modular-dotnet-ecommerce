namespace Architecture.Tests;

/// <summary>
/// Inside a module: Domain is the core and depends on nothing but BuildingBlocks.
/// (Features -> Infrastructure is allowed: handlers use the DbContext directly, and
/// Infrastructure -> Features is allowed for DI registration.)
/// </summary>
public class LayerTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Domain_does_not_depend_on_Features_or_Infrastructure(string module)
    {
        var domain = Modules.Types(module).That().ResideInNamespace($"{module}.Domain");
        RuleAssert.NotEmpty(domain, $"types in {module}.Domain");

        RuleAssert.Passes(
            domain.ShouldNot().HaveDependencyOnAny($"{module}.Features", $"{module}.Infrastructure"),
            $"{module}.Domain must not depend on the application or infrastructure layers.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Domain_does_not_depend_on_frameworks(string module)
    {
        // MediatR is deliberately not listed: BuildingBlocks.Domain.IDomainEvent extends INotification.
        string[] frameworks =
        [
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "MassTransit",
            "FluentValidation",
            "Grpc",
            "Npgsql",
        ];

        RuleAssert.Passes(
            Modules.Types(module).That().ResideInNamespace($"{module}.Domain")
                .ShouldNot().HaveDependencyOnAny(frameworks),
            $"{module}.Domain must stay persistence- and transport-agnostic.");
    }
}
