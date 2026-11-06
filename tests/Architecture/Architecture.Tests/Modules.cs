using System.Reflection;
using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>
/// The modules under test. Each module ships an implementation assembly and a Contracts
/// assembly; other modules may only talk to it through the Contracts assembly.
/// Add a module here when it gets real code (Orders, Payments, ...) and every rule picks it up.
/// </summary>
public static class Modules
{
    public const string BuildingBlocks = "BuildingBlocks";

    public static readonly string[] Names = ["Cart", "Catalog", "Inventory"];

    /// <summary>xUnit MemberData source: one row per module.</summary>
    public static TheoryData<string> All => new(Names);

    public static Assembly Assembly(string module) => System.Reflection.Assembly.Load(module);

    public static Assembly ContractsAssembly(string module) =>
        System.Reflection.Assembly.Load($"{module}.Contracts");

    public static Types Types(string module) => NetArchTest.Rules.Types.InAssembly(Assembly(module));

    /// <summary>
    /// Namespaces that make up a module's internals: everything except its Contracts.
    /// Listed explicitly because a bare "Catalog" prefix would also match "Catalog.Contracts".
    /// </summary>
    public static string[] InternalNamespaces(string module) =>
    [
        $"{module}.Domain",
        $"{module}.Features",
        $"{module}.Infrastructure",
        $"{module}.{module}Module",
    ];
}
