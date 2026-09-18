using System.Text.RegularExpressions;
using Wolfe.Lab.Build.Backup.Models;

namespace Wolfe.Lab.Build.Restic;

/// <summary>
/// Runs the restic binary. The repository reaches it through the environment, never the
/// argument list, so the password is in no process table.
/// </summary>
internal sealed partial class ResticClient(ICommandRunner commands) : IRestic
{
    /// <inheritdoc />
    public async Task<Snapshot> Backup(ResticRepository repository, IReadOnlyList<IDirectory> paths, IReadOnlyList<string> excludes, IReadOnlyList<string> tags, CancellationToken ct = default)
    {
        var result = await commands.Run(BackupCommand(repository, paths, excludes, tags, dryRun: false), ct);
        var saved = SavedLine().Match(result.StandardOutput);
        if (!saved.Success)
        {
            throw new CommandFailedException("restic exited cleanly but reported no saved snapshot.", result);
        }

        return new Snapshot(saved.Groups["id"].Value);
    }

    /// <inheritdoc />
    public async Task Forget(ResticRepository repository, Snapshot snapshot, CancellationToken ct = default) =>
        await commands.Run(
            Command.Create("restic").WithArguments("forget", snapshot.Id).WithEnvironmentVariables(repository.Environment).QuietOutput().ThrowOnError(),
            ct);

    /// <summary>
    /// The backup invocation, shared with the rehearsal: restic's own <c>--dry-run</c> reads the
    /// repository and the paths and writes nothing.
    /// </summary>
    internal static Command BackupCommand(ResticRepository repository, IReadOnlyList<IDirectory> paths, IReadOnlyList<string> excludes, IReadOnlyList<string> tags, bool dryRun)
    {
        var arguments = new List<string> { "backup" };
        if (dryRun)
        {
            arguments.AddRange(["--dry-run", "--verbose"]);
        }

        arguments.AddRange(paths.Select(p => p.AbsolutePath));
        foreach (var exclude in excludes)
        {
            arguments.AddRange(["--exclude", exclude]);
        }

        foreach (var tag in tags)
        {
            arguments.AddRange(["--tag", tag]);
        }

        return Command.Create("restic").WithArguments([.. arguments]).WithEnvironmentVariables(repository.Environment).ThrowOnError();
    }

    [GeneratedRegex(@"^snapshot (?<id>[0-9a-f]+) saved$", RegexOptions.Multiline)]
    private static partial Regex SavedLine();
}
