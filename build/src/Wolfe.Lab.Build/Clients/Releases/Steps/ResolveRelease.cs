using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Build.Clients.Releases.Steps;

/// <summary>
/// Works out where the component is installed on this node.
/// </summary>
[Step("resolve release", StepKind.Work)]
internal sealed class ResolveRelease(ReleaseName name, WorkflowEnvironment environment, IWorkflowLog log)
{
    /// <summary>
    /// Overrides the release root, for a node that keeps its installs elsewhere.
    /// </summary>
    internal const string RootVariable = "LAB_ROOT";

    public StepResult<Release> Run()
    {
        var root = environment.Get(RootVariable) is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Wolfe.Lab");

        var release = new Release(name.Value, new PhysicalDirectory(Path.Combine(root, name.Value)));
        log.Detail($"{release.Name} releases to {release.Directory.AbsolutePath}.");
        return release;
    }
}
