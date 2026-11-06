using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>
/// Modules are isolated from each other: a module may reference another module's Contracts,
/// never its implementation.
/// </summary>
public class ModuleBoundaryTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_does_not_reference_other_module_assemblies(string module)
    {
        var forbidden = Modules.Names.Where(m => m != module).ToHashSet();

        var offending = Modules.Assembly(module)
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(forbidden.Contains)
            .ToList();

        Assert.True(offending.Count == 0,
            $"{module} references other module implementations: {string.Join(", ", offending)}. " +
            "Reference their *.Contracts project instead.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_types_do_not_depend_on_other_module_internals(string module)
    {
        var forbidden = Modules.Names
            .Where(m => m != module)
            .SelectMany(Modules.InternalNamespaces)
            .ToArray();

        RuleAssert.Passes(
            Modules.Types(module).ShouldNot().HaveDependencyOnAny(forbidden),
            $"{module} must only use other modules through their Contracts.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Contracts_do_not_depend_on_any_module_implementation(string module)
    {
        var forbidden = Modules.Names.SelectMany(Modules.InternalNamespaces).ToArray();

        RuleAssert.Passes(
            Types.InAssembly(Modules.ContractsAssembly(module)).ShouldNot().HaveDependencyOnAny(forbidden),
            $"{module}.Contracts is shared with other modules and must not leak implementation types.");
    }

    [Fact]
    public void BuildingBlocks_does_not_depend_on_any_module()
    {
        var forbidden = Modules.Names
            .SelectMany(m => Modules.InternalNamespaces(m).Append($"{m}.Contracts"))
            .ToArray();

        RuleAssert.Passes(
            Types.InAssembly(System.Reflection.Assembly.Load(Modules.BuildingBlocks))
                .ShouldNot().HaveDependencyOnAny(forbidden),
            "BuildingBlocks is shared infrastructure and must not know about any module.");
    }
}
