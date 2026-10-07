using OpenTelemetry.Resources;
using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Application.Telemetry;

/// <summary>
/// Labels a run's telemetry with where its directory's components live.
/// </summary>
public sealed class ComponentResourceDetector(IGit git, IFileSystem fileSystem) : IResourceDetector
{
    // OpenTelemetry asks once, synchronously, as the run starts.
    /// <inheritdoc />
    public Resource Detect() => new(Attributes().GetAwaiter().GetResult());

    private async Task<IEnumerable<KeyValuePair<string, object>>> Attributes()
    {
        if (await Unit() is not { } unit)
        {
            return [];
        }

        // What every component in the directory agrees on.
        return unit.Components
            .SelectMany(component => component.Attributes)
            .GroupBy(attribute => attribute.Key.Name, attribute => attribute.Value)
            .Where(values => values.Distinct().Count() == 1)
            .Select(values => KeyValuePair.Create(values.Key, (object)values.First()));
    }

    // Outside a checkout, or where nothing is declared, the run is no component's.
    private async Task<DeploymentUnit?> Unit()
    {
        if (await git.RepositoryRoot() is not { } root
            || RepositoryPath.TryFrom(root.RelativePath(fileSystem.ProjectRoot)) is not { IsSuccess: true } path)
        {
            return null;
        }

        return (await ServiceCatalogReader.Read(git, root)).TryGetValue(out var catalog, out _) ? catalog.DeploymentUnitAt(path.ValueObject)?.Value : null;
    }
}
