using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Deploy.Models;

namespace Wolfe.Lab.Build.Deploy.Steps;

/// <summary>
/// Gets the slice from the directory the job runs in.
/// </summary>
[Step("resolve slice", StepKind.Work)]
internal sealed class ResolveSlice(IFileSystem fileSystem, WorkflowEnvironment environment)
{
    public StepResult<Slice> Run()
    {
        var source = fileSystem.ProjectRoot;
        var root = environment.Get("LAB_ROOT") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Wolfe.Lab");
        return new Slice(source.Name, source, new PhysicalDirectory(Path.Combine(root, source.Name)));
    }
}
