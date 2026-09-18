using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Wolfe.Lab.Build.Backup.Models;
using Wolfe.Lab.Build.Restic.Models;

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

    /// <inheritdoc />
    public async Task Copy(OffsiteRepository offsite, CancellationToken ct = default) =>
        await commands.Run(Command.Create("restic").WithArguments("copy").WithEnvironmentVariables(offsite.Repository.Environment).ThrowOnError(), ct);

    /// <inheritdoc />
    public async Task Prune(ResticRepository repository, RetentionPolicy policy, CancellationToken ct = default) =>
        await commands.Run(PruneCommand(repository, policy, dryRun: false), ct);

    /// <inheritdoc />
    public async Task Check(ResticRepository repository, string? readDataSubset, CancellationToken ct = default) =>
        await commands.Run(CheckCommand(repository, readDataSubset), ct);

    /// <inheritdoc />
    public async Task<ResticSnapshot?> FindSnapshot(ResticRepository repository, string tag, string? id, CancellationToken ct = default)
    {
        var command = Command.Create("restic").WithArguments("snapshots", "--json", "--tag", tag);
        command = id is null ? command.AndArguments("--latest", "1") : command.AndArguments(id);
        var result = await commands.Run(command.WithEnvironmentVariables(repository.Environment).QuietOutput().ThrowOnError(), ct);
        var listed = JsonSerializer.Deserialize<List<ListedSnapshot>>(result.StandardOutput, ListingOptions) ?? [];
        return listed.Select(s => new ResticSnapshot(s.ShortId, s.Time, s.Tags ?? [], s.Paths ?? [])).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task Restore(ResticRepository repository, string snapshotId, IDirectory target, IReadOnlyList<string> includes, CancellationToken ct = default)
    {
        var arguments = new List<string> { "restore", snapshotId, "--target", target.AbsolutePath };
        foreach (var include in includes)
        {
            arguments.AddRange(["--include", include]);
        }

        await commands.Run(Command.Create("restic").WithArguments([.. arguments]).WithEnvironmentVariables(repository.Environment).ThrowOnError(), ct);
    }

    private static readonly JsonSerializerOptions ListingOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>
    /// A snapshot as <c>restic snapshots --json</c> prints it, the fields the lab reads.
    /// </summary>
    private sealed record ListedSnapshot(
        [property: JsonPropertyName("short_id")] string ShortId,
        DateTimeOffset Time,
        IReadOnlyList<string>? Tags,
        IReadOnlyList<string>? Paths);

    /// <summary>
    /// The prune invocation, shared with the rehearsal: restic's own <c>--dry-run</c> says what
    /// forget would remove and prunes nothing.
    /// </summary>
    internal static Command PruneCommand(ResticRepository repository, RetentionPolicy policy, bool dryRun)
    {
        var arguments = new List<string>
        {
            "forget",
            "--keep-daily", policy.Daily.ToString(CultureInfo.InvariantCulture),
            "--keep-weekly", policy.Weekly.ToString(CultureInfo.InvariantCulture),
            "--keep-monthly", policy.Monthly.ToString(CultureInfo.InvariantCulture)
        };
        foreach (var tag in policy.KeepTags)
        {
            arguments.AddRange(["--keep-tag", tag]);
        }

        arguments.Add(dryRun ? "--dry-run" : "--prune");
        return Command.Create("restic").WithArguments([.. arguments]).WithEnvironmentVariables(repository.Environment).ThrowOnError();
    }

    /// <summary>
    /// The check invocation.
    /// </summary>
    internal static Command CheckCommand(ResticRepository repository, string? readDataSubset)
    {
        var command = Command.Create("restic").WithArguments("check");
        if (readDataSubset is not null)
        {
            command = command.AndArguments($"--read-data-subset={readDataSubset}");
        }

        return command.WithEnvironmentVariables(repository.Environment).ThrowOnError();
    }

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
