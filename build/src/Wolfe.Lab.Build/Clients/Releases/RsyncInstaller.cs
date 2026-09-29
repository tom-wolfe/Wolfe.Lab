namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// rsync, so a running container never sees its directory vanish the way a delete-and-copy
/// would make it.
/// </summary>
internal sealed class RsyncInstaller(ICommandRunner commands) : IReleaseInstaller
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Install(IDirectory source, IDirectory release, CancellationToken ct = default)
    {
        release.Create();
        var result = await commands.Run(Rsync(source, release).QuietOutput().ThrowOnError(), ct);
        return Changes(result.StandardOutput);
    }

    /// <summary>
    /// The trailing slash on the source copies its contents rather than the directory itself.
    /// The declaration stays behind: the node runs the component, it does not run jobs on it.
    /// </summary>
    internal static Command Rsync(IDirectory source, IDirectory release, bool dryRun = false)
    {
        var command = Command.Create("rsync")
            .WithArguments("-rlp", "--checksum", "--delete", "--exclude", "ritten.json");

        if (dryRun)
        {
            command = command.AndArguments("--dry-run", "--itemize-changes");
        }

        return command.AndArguments($"{source.AbsolutePath}/", $"{release.AbsolutePath}/");
    }

    /// <summary>
    /// rsync's itemised output as the files it changed. A line leading with <c>.</c> changed
    /// only attributes, and a directory is the files in it, so both are left out; the rest are
    /// a transfer (<c>&gt;</c>), a creation (<c>c</c>) or <c>*deleting</c>. The flag column is read
    /// up to its first space, since GNU rsync and macOS's openrsync print different widths.
    /// </summary>
    internal static IReadOnlyList<string> Changes(string itemized)
    {
        var changes = new List<string>();
        foreach (var line in itemized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var space = line.IndexOf(' ');
            if (space < 0)
            {
                continue;
            }

            var flags = line[..space];
            var path = line[space..].Trim();
            if (flags == "*deleting")
            {
                changes.Add($"- {path}");
            }
            else if (flags.Length > 2 && flags[0] != '.' && flags[1] != 'd')
            {
                changes.Add(flags[2..].All(flag => flag == '+') ? $"+ {path}" : $"~ {path}");
            }
        }

        return changes;
    }
}
