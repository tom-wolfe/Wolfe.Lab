namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// One env file entry: a literal, or a reference not yet read.
/// </summary>
public abstract record EnvValue
{
    private EnvValue()
    {
    }

    /// <summary>
    /// A value as written.
    /// </summary>
    /// <param name="Value">The value.</param>
    public sealed record Literal(string Value) : EnvValue;

    /// <summary>
    /// A vault reference, read at the moment of use.
    /// </summary>
    /// <param name="Reference">The reference.</param>
    public sealed record Secret(SecretReference Reference) : EnvValue;
}
