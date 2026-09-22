using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Works out where this component's source is and where it installs to.
/// </summary>
/// <remarks>
/// Like the slice-level resolve, except the release name can be declared: a component
/// directory is named for what it is (<c>compose</c>), not for what it belongs to, and the
/// install root is flat.
/// </remarks>
[Step("resolve stack", StepKind.Work)]
internal sealed class ResolveStack(DockerSettings settings, IFileSystem fileSystem, WorkflowEnvironment environment)
{
    public StepResult<Slice> Run()
    {
        var source = fileSystem.ProjectRoot;
        var root = environment.Get("LAB_ROOT") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Wolfe.Lab");

        var name = settings.Release is { Length: > 0 } declared ? declared : source.Name;
        return new Slice(name, source, new PhysicalDirectory(Path.Combine(root, name)));
    }
}
