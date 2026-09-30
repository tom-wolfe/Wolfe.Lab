using Ritten.Git;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Clients.Releases.Steps;

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

        if (Component.From(checkout, dir) is not { } component)
        {
            return new Error($"{Path.GetRelativePath(checkout.AbsolutePath, dir.AbsolutePath)} is not <area>/<service>/<component>, so what it runs cannot say where it lives.");
        }

        log.Detail($"{component.Name} lives at {component}.");
        return component;
    }
}
