using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Works out where the component is installed on this node.
/// </summary>
[Step("resolve release", StepKind.Work)]
internal sealed class ResolveRelease(ReleaseName name, IOptions<LabDirectories> roots, IWorkflowLog log)
{
    public StepResult<Release> Run()
    {
        var release = new Release(name.Value, roots.Value.Root.GetDirectory(name.Value));
        log.Detail($"{release.Name} releases to {release.Directory.AbsolutePath}.");
        return release;
    }
}
