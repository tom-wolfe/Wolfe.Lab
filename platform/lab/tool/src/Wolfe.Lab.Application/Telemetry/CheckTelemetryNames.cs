using Ritten.Docker;
using Ritten.Git;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Application.Telemetry;

/// <summary>
/// Holds every file of the component that names the lab's telemetry to the lab's one list of it:
/// its compose file, its collector's configuration, its stores' and its log targets.
/// </summary>
[Step("check telemetry names", StepKind.Check)]
internal sealed class CheckTelemetryNames(IGit git, IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var problems = await Check(git, docker, fileSystem.ProjectRoot, ct);
        if (problems.Count > 0)
        {
            return StepResult.Failed(problems);
        }

        log.Detail("Every telemetry name is one the lab knows.");
        return StepResult.Successful;
    }

    /// <summary>
    /// What is wrong with the component's files, each problem prefixed with its file. Only what
    /// git tracks is read: the component as it is committed, not whatever lies beside it — a
    /// run's report, a tool's scratch, a build's output.
    /// </summary>
    internal static async Task<IReadOnlyList<Error>> Check(IGit git, IDocker docker, IDirectory component, CancellationToken ct = default)
    {
        var problems = new List<Error>();
        var tracked = (await git.InRepository(component).TrackedFiles(ct: ct)).Order(StringComparer.Ordinal).ToList();
        foreach (var file in tracked)
        {
            var errors = await Read(component, file, ct);
            problems.AddRange(errors.Select(error => new Error($"{file}: {error.Message}")));
        }

        // A stack is read once, as compose resolves it, whichever of its files it is spread over.
        if (ComposeFiles.Defaults.FirstOrDefault(tracked.Contains) is { } stack)
        {
            var project = await docker.ComposeConfig(component, ct: ct);
            problems.AddRange((project.Value is { } read ? ComposeTelemetry.From(read).Errors ?? [] : project.Errors ?? [])
                .Select(error => new Error($"{stack}: {error.Message}")));
        }

        return problems;
    }

    /// <summary>
    /// What one file names wrongly, read as the kind of file it is.
    /// </summary>
    private static async Task<IReadOnlyList<Error>> Read(IDirectory component, string file, CancellationToken ct)
    {
        var target = component.GetFile(file);
        var name = target.Name;
        var extension = target.Extension;
        var isTargets = name.EndsWith(".json", StringComparison.Ordinal) || name.EndsWith(".json.tmpl", StringComparison.Ordinal);
        if (!isTargets && extension is not (".alloy" or ".yaml" or ".yml"))
        {
            return [];
        }

        // Tracked, but deleted in the working directory: nothing to name anything.
        if (await target.ReadAllTextIfExists(ct) is not { } text)
        {
            return [];
        }

        if (isTargets)
        {
            // chezmoi's copy is a template of one, which reads as one once it is JSON.
            return LogTargetFile.Read(text).Errors ?? [];
        }

        if (extension == ".alloy")
        {
            return AlloyConfig.Read(text).Errors ?? [];
        }

        return [.. StoreConfig.Read(text).Errors ?? [], .. LabelMentions.In(text).Errors ?? []];
    }
}
