using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Checkout;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Application.Catalog;

/// <summary>
/// Resolves and verifies the component's catalog definition against the rest of the lab.
/// </summary>
[Step("resolve service catalog", StepKind.Check)]
internal sealed class ResolveServiceCatalog(IGit git, IFileSystem fileSystem, SelectedWorkflow selected, IWorkflowLog log)
{
    public async Task<StepResult<ServiceCatalog>> Run(CancellationToken ct = default)
    {
        if (await git.RepositoryRoot(ct) is not { } repository)
        {
            return RepositoryErrors.NotInARepository(fileSystem.ProjectRoot);
        }

        var directory = fileSystem.ProjectRoot;
        // The workflow's name, as a declaration writes it: its label is how a run prints it.
        if (!(await Check(git, repository, directory, selected.Workflow.Name, ct)).TryGetValue(out var catalog, out var errors))
        {
            return StepResult.Failed(errors);
        }

        log.Detail(Placed(repository, directory) is { } placement && catalog.DeploymentUnitAt(placement)?.Value is { } unit
            ? $"{unit} declares {string.Join(", ", unit.Components.Select(component => $"{component.Name} ({component.Kind}, {component.Workflow})"))}, and each holds."
            : "The directory declares no components yet.");
        return catalog;
    }

    /// <summary>
    /// The catalog, when every declaration holds and the directory's are those of its workflow, or
    /// every problem: one anywhere fails every check, as it would fail every deploy.
    /// </summary>
    /// <param name="git">What lists the checkout's files.</param>
    /// <param name="root">The checkout's root.</param>
    /// <param name="directory">The directory the workflow runs in.</param>
    /// <param name="workflow">The workflow running this check: the one its <c>ritten.json</c> names, or its components declare.</param>
    /// <param name="ct">A token to monitor for cancellation.</param>
    internal static async Task<Result<ServiceCatalog>> Check(IGit git, IDirectory root, IDirectory directory, string workflow, CancellationToken ct = default)
    {
        if (!(await ServiceCatalogReader.Read(git, root, ct)).TryGetValue(out var catalog, out var errors))
        {
            return errors;
        }

        // A directory outside the checkout has no declaration this check could find.
        if (Placed(root, directory) is not { } placement)
        {
            return catalog;
        }

        // Where a ritten.json names the directory's workflow too, each component must declare it; a
        // part of another — anything partOf one — names its own.
        var others = catalog.DeploymentUnitAt(placement)?.Value?.Components
            .Where(component => component.PartOf is null && component.Workflow.Value != workflow)
            .ToList() ?? [];
        return others.Count == 0
            ? catalog
            : others.Select(Error (component) => CatalogError.In(component.Source, WorkflowErrors.NotOwnedByTheDirectory(component.Workflow, workflow))).ToList();
    }

    private static RepositoryPath? Placed(IDirectory root, IDirectory directory) =>
        RepositoryPath.TryFrom(root.RelativePath(directory)) is { IsSuccess: true } path ? path.ValueObject : null;
}
