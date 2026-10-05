namespace Wolfe.Lab.Domain.Catalog.Components.Agents;

/// <summary>
/// The name an agent runs under on its node — its unit is <c>dev.twolfe.&lt;name&gt;</c> — and the
/// <c>service_name</c> its logs carry: <c>alloy</c>, <c>beszel-agent</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct AgentName
{
    private static Validation Validate(string input) => NameRule.Validate(input, "an agent's name");
}
