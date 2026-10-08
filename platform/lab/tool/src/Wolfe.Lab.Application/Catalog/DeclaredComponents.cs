using Ritten.Git;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Application.Catalog;

/// <summary>
/// The component the working directory declares, while a workflow's settings may still be its
/// <c>ritten.json</c>'s instead (ROADMAP #14, step 7): gone once every directory declares them.
/// </summary>
internal sealed class DeclaredComponents(IGit git, IFileSystem fileSystem)
{
    /// <summary>
    /// The directory's component of type <typeparamref name="T"/>, or null while it declares none.
    /// </summary>
    public async Task<T?> Find<T>(ServiceCatalog catalog, CancellationToken ct = default) where T : Component =>
        await git.RepositoryRoot(ct) is { } root && RepositoryPath.TryFrom(root.RelativePath(fileSystem.ProjectRoot)) is { IsSuccess: true } path
            ? catalog.DeploymentUnitAt(path.ValueObject)?.Value?.Components.OfType<T>().FirstOrDefault()
            : null;
}
