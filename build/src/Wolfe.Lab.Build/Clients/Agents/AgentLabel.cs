using Vogen;

namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// A supervised agent's label — <c>dev.twolfe.ollama</c>. The prefix is the lab's, so an agent
/// is named by the key that declares it and never spelled out by hand.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct AgentLabel
{
    /// <summary>
    /// The prefix every lab agent's label carries.
    /// </summary>
    public const string Prefix = "dev.twolfe.";

    /// <summary>
    /// The name the label was built from, without the lab's prefix.
    /// </summary>
    public string Name => Value[Prefix.Length..];

    /// <summary>
    /// The label for an agent declared under <paramref name="name"/>, or null when the name
    /// cannot be one: the key comes from JSON, so this is asked rather than assumed. A dot is
    /// refused along with the rest so the label stays the three segments it reads as.
    /// </summary>
    /// <param name="name">The key the agent is declared under.</param>
    public static AgentLabel? ForName(string name) =>
        name.Length > 0 && !name.Contains('.') && !name.Contains('/') && !name.Any(char.IsWhiteSpace)
            ? From(Prefix + name)
            : null;

    private static string NormalizeInput(string input) => input.Trim();

    private static Validation Validate(string input) =>
        input.StartsWith(Prefix, StringComparison.Ordinal) && input.Length > Prefix.Length
        && !input.Any(char.IsWhiteSpace) && !input.Contains('/')
            ? Validation.Ok
            : Validation.Invalid($"'{input}' must be {Prefix}<name>, with no whitespace or '/'.");
}
