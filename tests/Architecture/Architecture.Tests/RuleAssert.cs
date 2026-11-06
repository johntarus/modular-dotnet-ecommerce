using NetArchTest.Rules;

namespace Architecture.Tests;

internal static class RuleAssert
{
    /// <summary>Fails with the full names of every offending type.</summary>
    public static void Passes(ConditionList rule, string because)
    {
        var result = rule.GetResult();
        if (result.IsSuccessful) return;

        var offenders = string.Join(Environment.NewLine, (result.FailingTypeNames ?? []).Select(n => $"  - {n}"));
        Assert.Fail($"{because}{Environment.NewLine}Offending types:{Environment.NewLine}{offenders}");
    }

    /// <summary>
    /// Guards against a rule passing vacuously because its type selection matched nothing
    /// (e.g. after a namespace rename).
    /// </summary>
    public static void NotEmpty(PredicateList selection, string what)
    {
        Assert.True(selection.GetTypes().Any(), $"Expected to find {what}, but the selection was empty.");
    }
}
