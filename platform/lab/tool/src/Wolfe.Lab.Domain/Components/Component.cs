using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Domain.Components;

/// <summary>
/// Encapsulates the metadata for a given component.
/// </summary>
/// <param name="Area">The area its service is classified under.</param>
/// <param name="Service">The service it belongs to.</param>
/// <param name="Name">Its own name, which is its directory's.</param>
public sealed record Component(AreaName Area, ServiceName Service, ComponentName Name)
{
    /// <summary>
    /// Where the component lives, as the attributes its telemetry is labelled with.
    /// </summary>
    public IReadOnlyList<KeyValuePair<TelemetryAttribute, string>> Attributes =>
    [
        new(TelemetryAttribute.Area, Area.Value),
        new(TelemetryAttribute.Service, Service.Value),
        new(TelemetryAttribute.Component, Name.Value)
    ];

    /// <summary>
    /// The attributes as OpenTelemetry's <c>OTEL_RESOURCE_ATTRIBUTES</c> spells them.
    /// </summary>
    public string ResourceAttributes => string.Join(',', Attributes.Select(attribute => $"{attribute.Key.Name}={attribute.Value}"));

    /// <summary>
    /// The placement as one name, for a file of the component's own: <c>ai-ollama-server</c>.
    /// </summary>
    public string QualifiedName => $"{Area}-{Service}-{Name}";

    /// <summary>
    /// Its directory in the repository: <c>area/service/component</c>.
    /// </summary>
    public RepositoryPath Directory => RepositoryPath.From($"{Area}/{Service}/{Name}");

    /// <summary>
    /// Loads a component from a directory.
    /// </summary>
    /// <param name="root">The checkout's root, as an absolute path.</param>
    /// <param name="component">The component's directory, as an absolute path.</param>
    public static Result<Component> From(string root, string component)
    {
        if (RepositoryPath.TryFrom(Path.GetRelativePath(root, component)) is not { IsSuccess: true } path
            || path.ValueObject.Segments is not [var area, var service, var name])
        {
            return new Error($"{component} is not a component: one sits three directories below the checkout's root, area/service/component.");
        }

        var (placedArea, placedService, placedName) = (AreaName.TryFrom(area), ServiceName.TryFrom(service), ComponentName.TryFrom(name));
        // Vogen's error on a success is Validation.Ok, not null: whether each one holds is asked.
        var errors = new[]
            {
                placedArea.IsSuccess ? null : placedArea.Error.ErrorMessage,
                placedService.IsSuccess ? null : placedService.Error.ErrorMessage,
                placedName.IsSuccess ? null : placedName.Error.ErrorMessage
            }
            .OfType<string>()
            .Select(message => new Error($"{path.ValueObject}: {message}"))
            .ToList();

        return errors.Count > 0 ? errors : new Component(placedArea.ValueObject, placedService.ValueObject, placedName.ValueObject);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Area}/{Service}/{Name}";
}
