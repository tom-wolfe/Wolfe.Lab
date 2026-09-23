namespace Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;

/// <summary>
/// What every registration shares.
/// </summary>
/// <param name="Vault">The vault that holds the registration secrets.</param>
/// <param name="Repository">The repository a host runner is scoped to.</param>
/// <param name="Image">The image a docker runner's jobs run in.</param>
public sealed record RunnerDefaults(string Vault, string Repository, string Image);
