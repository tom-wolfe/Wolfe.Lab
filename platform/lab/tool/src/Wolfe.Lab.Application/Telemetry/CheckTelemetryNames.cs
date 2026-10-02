using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Application.Telemetry;

/// <summary>
/// Holds every file of the component that names the lab's telemetry to the lab's one list of it:
/// its compose file, its collector's configuration, its stores' and its log targets.
/// </summary>
[Step("check telemetry names", StepKind.Check)]
internal sealed class CheckTelemetryNames(ICommandRunner commands, IFileSystem fileSystem, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var problems = await Check(commands, fileSystem.ProjectRoot.AbsolutePath, ct);
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
    internal static async Task<IReadOnlyList<Error>> Check(ICommandRunner commands, string component, CancellationToken ct = default)
    {
        var problems = new List<Error>();
        foreach (var file in await Tracked(commands, component, ct))
        {
            var errors = await Read(commands, component, file, ct);
            problems.AddRange(errors.Select(error => new Error($"{file}: {error.Message}")));
        }

        return problems;
    }

    /// <summary>
    /// What one file names wrongly, read as the kind of file it is.
    /// </summary>
    private static async Task<IReadOnlyList<Error>> Read(ICommandRunner commands, string component, string file, CancellationToken ct)
    {
        var name = Path.GetFileName(file);
        var extension = Path.GetExtension(file);
        var isTargets = name.EndsWith(".json", StringComparison.Ordinal) || name.EndsWith(".json.tmpl", StringComparison.Ordinal);
        if (!isTargets && extension is not (".alloy" or ".yaml" or ".yml"))
        {
            return [];
        }

        var text = await File.ReadAllTextAsync(Path.Combine(component, file), ct);
        if (isTargets)
        {
            // chezmoi's copy is a template of one, which reads as one once it is JSON.
            return LogTargetFile.Read(text).Errors ?? [];
        }

        if (extension == ".alloy")
        {
            return AlloyConfig.Read(text).Errors ?? [];
        }

        var errors = new List<Error>([.. StoreConfig.Read(text).Errors ?? [], .. LabelMentions.In(text).Errors ?? []]);
        if (file == ComposeProject.FileName)
        {
            var project = await ComposeProject.Read(commands, component, ct);
            errors.AddRange(project.Value is { } read ? ComposeTelemetry.From(read).Errors ?? [] : project.Errors ?? []);
        }

        return errors;
    }

    /// <summary>
    /// The files git tracks in the component, relative to it.
    /// </summary>
    private static async Task<IReadOnlyList<string>> Tracked(ICommandRunner commands, string component, CancellationToken ct)
    {
        var result = await commands.Run(Command.Create("git").WithArguments("ls-files", "-z").InDirectory(component).QuietOutput().ThrowOnError(), ct);
        return [.. result.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal)];
    }
}
