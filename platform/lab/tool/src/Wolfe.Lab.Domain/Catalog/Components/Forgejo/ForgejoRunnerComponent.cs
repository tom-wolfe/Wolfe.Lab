namespace Wolfe.Lab.Domain.Catalog.Components.Forgejo;

/// <summary>
/// A component the <c>forgejo-runner</c> workflow operates: the Actions runners registered with
/// Forgejo, a host runner and a Docker runner per node.
/// </summary>
public sealed class ForgejoRunnerComponent : Component
{
    private ForgejoRunnerComponent() { }

    /// <summary>
    /// The 1Password vault each runner's registration secret is kept in.
    /// </summary>
    public required string Vault { get; init; }

    /// <summary>
    /// The repository a host runner is scoped to: <c>tom-wolfe/Wolfe.Lab</c>.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// The image a Docker runner's jobs run in by default.
    /// </summary>
    public required string Image { get; init; }

    /// <summary>
    /// Creates a new runners component.
    /// </summary>
    public static Result<ForgejoRunnerComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, string vault, string repository, string image)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new ForgejoRunnerComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.ForgejoRunner,
            Vault = vault,
            Repository = repository,
            Image = image
        };
    }
}
