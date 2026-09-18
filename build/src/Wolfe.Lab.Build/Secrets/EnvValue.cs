namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// One env file entry: a literal, or a reference not yet read. Exactly one is set.
/// </summary>
/// <param name="Literal">The value as written, when it is not a reference.</param>
/// <param name="Reference">The vault reference, when it is one.</param>
public sealed record EnvValue(string? Literal, SecretReference? Reference);
