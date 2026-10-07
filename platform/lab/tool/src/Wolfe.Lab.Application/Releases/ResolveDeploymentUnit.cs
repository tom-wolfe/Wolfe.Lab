using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Checkout;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Finds 'the thing' that we're working with in this workflow.
/// </summary>
[Step("resolve deployment unit", StepKind.Work)]
internal sealed class ResolveDeploymentUnit(IGit git, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult<DeploymentUnit>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        var dir = fileSystem.ProjectRoot;
        if (await git.RepositoryRoot(ct) is not { } repository)
        {
            return RepositoryErrors.NotInARepository(dir);
        }

        if (RepositoryPath.TryFrom(repository.RelativePath(dir)) is not { IsSuccess: true } path)
        {
            return RepositoryErrors.OutsideTheCheckout(dir, repository);
        }

        if (catalog.DeploymentUnitAt(path.ValueObject) is not { } declared)
        {
            return DeploymentUnitErrors.NothingDeclared(path.ValueObject);
        }

        if (!declared.TryGetValue(out var unit, out var errors))
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"{unit} deploys {string.Join(", ", unit.Components.Select(component => component.Name))} of {unit.Service}, as {unit.Name}.");
        return unit;
    }
}
