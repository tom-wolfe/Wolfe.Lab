namespace Wolfe.Lab.Domain.Catalog.Components.Chezmoi;

/// <summary>
/// A component the <c>chezmoi</c> workflow operates: the machines' profiles, rendered and checked
/// before any node applies them.
/// </summary>
public sealed class ChezmoiComponent : Component
{
    private ChezmoiComponent() { }

    /// <summary>
    /// The profiles a check renders.
    /// </summary>
    public required IReadOnlyList<ChezmoiProfile> Profiles { get; init; }

    /// <summary>
    /// Creates a new chezmoi component.
    /// </summary>
    public static Result<ChezmoiComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, IReadOnlyList<ChezmoiProfile> profiles)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new ChezmoiComponent { Source = source, Directory = directory, Name = name, Kind = kind, Workflow = WorkflowName.Chezmoi, Profiles = profiles };
    }
}
