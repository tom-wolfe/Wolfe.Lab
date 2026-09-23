namespace Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;

/// <summary>
/// The shape of the runners component's <c>ritten.json</c>: <c>"workflow": "forgejo-runners"</c>.
/// </summary>
public sealed record ForgejoRunnersSettings : WorkflowSettings
{
    /// <summary>
    /// The vault that holds each runner's registration secret, as <c>forgejo-runner-&lt;name&gt;</c>.
    /// </summary>
    public string? Vault { get; init; }

    /// <summary>
    /// The repository a host runner is scoped to.
    /// </summary>
    public string? Repository { get; init; }

    /// <summary>
    /// The image a docker runner's jobs run in.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// The settings as the steps consume them, or null while a field is missing.
    /// </summary>
    public RunnerDefaults? ToDefaults() =>
        Vault is { Length: > 0 } vault && Repository is { Length: > 0 } repository && Image is { Length: > 0 } image
            ? new RunnerDefaults(vault, repository, image)
            : null;
}
