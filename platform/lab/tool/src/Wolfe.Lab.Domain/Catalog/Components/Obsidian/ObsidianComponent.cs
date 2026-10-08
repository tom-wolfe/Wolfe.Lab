using Wolfe.Lab.Domain.Git;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Components.Obsidian;

/// <summary>
/// A component the <c>obsidian</c> workflow operates: a vault, pulled from Obsidian Sync and pushed
/// to its git repository.
/// </summary>
public sealed class ObsidianComponent : Component
{
    private ObsidianComponent() { }

    /// <summary>
    /// Where the vault is checked out on the node.
    /// </summary>
    public required HostPath Path { get; init; }

    /// <summary>
    /// The repository it is pushed to.
    /// </summary>
    public required RepositoryUrl Repository { get; init; }

    /// <summary>
    /// What the push authenticates with.
    /// </summary>
    public required PushCredential Push { get; init; }

    /// <summary>
    /// What a commit leaves out, as git's exclude patterns.
    /// </summary>
    public IReadOnlyList<string> Exclude { get; set; } = [];

    /// <summary>
    /// Creates a new vault component.
    /// </summary>
    public static Result<ObsidianComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, HostPath path, RepositoryUrl repository, PushCredential push)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new ObsidianComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.Obsidian,
            Path = path,
            Repository = repository,
            Push = push
        };
    }
}
