namespace Wolfe.Lab.Domain.Catalog.Components;

/// <summary>
/// The workflow that manages a component's maintenance operations: deployments, backups, etc.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("Docker", "docker", "A service of a compose stack, converged by the lab.")]
[Instance("DotNetService", "dotnet-service", "A service of a compose stack whose image the lab builds from .NET source.")]
[Instance("Garage", "garage", "Garage's service of its compose stack, and its node's place in the cluster layout.")]
[Instance("Agent", "agent", "A host process under launchd or systemd.")]
[Instance("Ollama", "ollama", "A model that can be used by the Ollama server.")]
[Instance("ForgejoRunner", "forgejo-runner", "A Forgejo Actions runner, registered by the lab.")]
[Instance("Obsidian", "obsidian", "An Obsidian vault, pushed to its git repository.")]
[Instance("Restic", "restic", "A restic repository: retention, verification, the offsite copy.")]
[Instance("Tofu", "tofu", "An OpenTofu root.")]
[Instance("CaddyCertificates", "caddy-certificates", "A certificate issued over ACME, kept renewed.")]
[Instance("Image", "image", "A container image, built and pushed.")]
[Instance("DotNetTool", "dotnet-tool", "A .NET tool, packed and pushed.")]
[Instance("Chezmoi", "chezmoi", "A node's profile, applied by chezmoi.")]
[Instance("Heartbeat", "heartbeat", "A dead man's switch: one ping, on a schedule.")]
[Instance("Backup", "backup", "A component's state, snapshotted into restic.")]
[Instance("GatusHealth", "gatus-health", "Gatus, asked on a schedule whether it is up.")]
[Instance("CaddyRoutes", "caddy-routes", "Every service's routes, gathered for the front door.")]
public readonly partial struct WorkflowName : IClosedSet<WorkflowName>
{
    /// <inheritdoc />
    public static IReadOnlyList<WorkflowName> All =>
        [Docker, DotNetService, Garage, Agent, Ollama, ForgejoRunner, Obsidian, Restic, Tofu, CaddyCertificates, Image, DotNetTool, Chezmoi, Backup, CaddyRoutes, Heartbeat, GatusHealth];

    /// <summary>
    /// True when this workflow runs the component as a process (a container or an agent).
    /// </summary>
    public bool IsHost => this == Docker || this == DotNetService || this == Garage || this == Agent || this == ForgejoRunner;

    private static Validation Validate(string input) =>
        All.Any(workflow => workflow.Value == input)
            ? Validation.Ok
            : Validation.Invalid(ComponentErrors.NotAWorkflow(input).Message);
}
