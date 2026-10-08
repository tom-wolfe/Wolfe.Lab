namespace Wolfe.Lab.Domain.Catalog.Components.Images;

/// <summary>
/// A component the <c>image</c> workflow operates: a container image, built from a Dockerfile and
/// pushed to its registry.
/// </summary>
public sealed class ImageComponent : Component
{
    private ImageComponent() { }

    /// <summary>
    /// Where it is pushed.
    /// </summary>
    public required ImageTag Tag { get; init; }

    /// <summary>
    /// The build's context, from the component's directory.
    /// </summary>
    public string Context { get; set; } = ".";

    /// <summary>
    /// Its Dockerfile, from the context.
    /// </summary>
    public string Dockerfile { get; set; } = "Dockerfile";

    /// <summary>
    /// Creates a new image component.
    /// </summary>
    public static Result<ImageComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, ImageTag tag)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new ImageComponent { Source = source, Directory = directory, Name = name, Kind = kind, Workflow = WorkflowName.Image, Tag = tag };
    }
}
