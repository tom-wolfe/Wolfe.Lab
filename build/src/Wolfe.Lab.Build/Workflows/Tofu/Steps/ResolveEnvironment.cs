using Ritten.Engine.FileSystem;
using Ritten.OpenTofu;

namespace Wolfe.Lab.Build.Workflows.Tofu.Steps;

/// <summary>
/// Names the env files the root's commands run under: the state backend every root shares, then
/// the root's own secrets.
/// </summary>
/// <remarks>
/// The state file is the CLI's, found by walking up from the root to the checkout's
/// <c>build/</c>: declared once, so a root cannot drift from the backend the others use. The
/// root's <c>secrets.env</c> comes second and may be absent — a root with no provider credential
/// has none. Both hold references; the values are read as each command starts.
/// </remarks>
[Step("resolve environment", StepKind.Work)]
internal sealed class ResolveEnvironment(IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string BuildDirectory = "build";
    internal const string StateFile = "tofu-state.env";
    internal const string SecretsFile = "secrets.env";

    public StepResult<TofuEnvironment> Run()
    {
        var root = fileSystem.ProjectRoot;
        if (FindStateFile(root) is not { } state)
        {
            return new Error($"No {BuildDirectory}/{StateFile} above {root.AbsolutePath}: the state backend every root shares is declared there.");
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

    private static IFile? FindStateFile(IDirectory from)
    {
        for (var directory = from; directory is not null; directory = Parent(directory))
        {
            var candidate = directory.GetDirectory(BuildDirectory).GetFile(StateFile);
            if (candidate.Exists)
            {
                return candidate;
            }
        }

        return null;
    }

    private static IDirectory? Parent(IDirectory directory) =>
        Path.GetDirectoryName(directory.AbsolutePath) is { Length: > 0 } parent && parent != directory.AbsolutePath
            ? new PhysicalDirectory(parent)
            : null;
}
