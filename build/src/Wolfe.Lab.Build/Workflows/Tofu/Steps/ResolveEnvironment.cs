using Ritten.OpenTofu;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Tofu.Steps;

/// <summary>
/// Names the env files the root's commands run under: the state backend every root shares, then
/// the root's own secrets.
/// </summary>
[Step("resolve environment", StepKind.Work)]
internal sealed class ResolveEnvironment(IFileSystem fileSystem, IWorkflowLog log)
{
    // Where the state file lived before it moved to Garage; dropped with the move.
    private static readonly Slice Former = new("build");

    internal const string StateFile = "tofu-state.env";
    internal const string SecretsFile = "secrets.env";

    public StepResult<TofuEnvironment> Run()
    {
        var root = fileSystem.ProjectRoot;
        if ((Slice.Garage.FindFile(root, StateFile) ?? Former.FindFile(root, StateFile)) is not { } state)
        {
            return new Error($"No {Slice.Garage}/{StateFile} above {root.AbsolutePath}: the state backend every root shares is declared there.");
        }

        var files = new List<IFile> { state };
        var secrets = root.GetFile(SecretsFile);
        if (secrets.Exists)
        {
            files.Add(secrets);
        }
        else
        {
            log.Detail($"{root.Name} names no secrets of its own.");
        }

        return new TofuEnvironment { EnvFiles = files };
    }
}
