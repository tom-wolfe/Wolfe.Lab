using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Application.Catalog;

/// <summary>
/// Verify's the component's catalog definition against the rest of the lab.
/// </summary>
[Step("check service catalog", StepKind.Check)]
internal sealed class CheckServiceCatalog(ICommandRunner commands, IGit git, IFileSystem fileSystem, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error($"{fileSystem.ProjectRoot.AbsolutePath} is not in a git checkout, and declarations are read from one.");
        }

        var problems = await Check(commands, checkout.AbsolutePath, fileSystem.ProjectRoot.AbsolutePath, job.Workflow, ct);
        if (!problems.TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        log.Detail(declared.Component is { } component
            ? $"{component.Source} declares {component.Declaration.Type}, and holds."
            : "The component has no declaration yet.");
        return StepResult.Successful;
    }

    /// <summary>
    /// The component's declaration — none, if it has none yet — when it holds, or its problems.
    /// </summary>
    /// <param name="commands">What runs git.</param>
    /// <param name="root">The checkout's root.</param>
    /// <param name="directory">The component's directory.</param>
    /// <param name="workflow">The workflow its <c>ritten.json</c> names, which is running this check.</param>
    /// <param name="ct">A token to monitor for cancellation.</param>
    internal static async Task<Result<Declared>> Check(ICommandRunner commands, string root, string directory, string workflow, CancellationToken ct = default)
    {
        // Where the component is placed is the deploy's to judge (ResolveComponent); one it
        // cannot place has no declaration this check could find.
        if (Domain.Components.Component.From(root, directory).Value is not { } placement)
        {
            return new Declared(null);
        }

        var reading = await DeclarationReader.Read(commands, root, ct);
        if (!reading.OfComponentIn(placement.Directory).TryGetValue(out var catalog, out var own))
        {
            return own;
        }

        if (catalog.ComponentAt(placement.Directory) is not { } component)
        {
            return new Declared(null);
        }

        if (!WorkflowTypes.Of(workflow).TryGetValue(out var runs, out var none))
        {
            return none.Select(error => new Error($"{component.Source}: this component runs {workflow}, but {error.Message}")).ToList();
        }

        return runs == component.Declaration.Type
            ? new Declared(component)
            : new Error($"{component.Source}: declares {component.Declaration.Type}, but its ritten.json runs the {workflow} workflow, which is {runs}.");
    }

    /// <summary>
    /// What a check found the component declares: its declaration, or null when it has none yet.
    /// </summary>
    internal sealed record Declared(CatalogComponent? Component);
}
