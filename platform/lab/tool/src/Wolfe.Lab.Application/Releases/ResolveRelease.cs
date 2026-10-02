using Ritten.Engine.FileSystem;

using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// Works out where the component is installed on this node.
/// </summary>
[Step("resolve release", StepKind.Work)]
internal sealed class ResolveRelease(ReleaseName name, WorkflowEnvironment environment, IWorkflowLog log)
{
    public StepResult<Release> Run()
    {
        var root = LabRoots.From(environment).Root;
        var release = new Release(name.Value, new PhysicalDirectory(Path.Combine(root, name.Value)));
        log.Detail($"{release.Name} releases to {release.Directory.AbsolutePath}.");
        return release;
    }
}
