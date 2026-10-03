using Ritten.Git;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Components;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Works out where in the lab the component lives, from its path in the checkout.
/// </summary>
[Step("resolve component", StepKind.Work)]
internal sealed class ResolveComponent(IGit git, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult<Component>> Run(CancellationToken ct = default)
    {
        var dir = fileSystem.ProjectRoot;
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error($"{dir.AbsolutePath} is not in a git checkout, and where a component lives is its path in one.");
        }

        // What it runs is labelled with where it lives, so a directory that cannot say is refused.
        if (!Component.From(checkout.AbsolutePath, dir.AbsolutePath).TryGetValue(out var component, out var errors))
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"{component.Name} lives at {component}.");
        return component;
    }
}
