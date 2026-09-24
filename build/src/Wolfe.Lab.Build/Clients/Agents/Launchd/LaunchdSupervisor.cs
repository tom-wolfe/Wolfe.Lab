using System.Globalization;
using System.Security;
using Scriban;

namespace Wolfe.Lab.Build.Clients.Agents.Launchd;

/// <summary>
/// launchd, through launchctl. Units are written to the user's LaunchAgents directory and
/// bootstrapped into the GUI domain.
/// </summary>
internal sealed class LaunchdSupervisor(AgentDirectory agents, ICommandRunner commands, IWorkflowLog log) : IServiceSupervisor
{
    private UserDomain? _domain;

    /// <inheritdoc />
    public AgentUnit Render(AgentDefinition agent)
    {
        var unit = new LaunchdUnit(
            Escape(agent.Label.Value),
            Escape(agent.Program.Value),
            agent.ProgramStamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            [.. agent.Arguments.Prepend(agent.Program.Value).Select(Escape)],
            agent.WorkingDirectory is { } workingDirectory ? Escape(workingDirectory.Value) : null,
            // Ordered, because the render is compared as text: a dictionary that came out of
            // JSON in a different order must not read as a changed agent.
            [.. agent.Environment
                .OrderBy(variable => variable.Key, StringComparer.Ordinal)
                .Select(variable => new UnitVariable(Escape(variable.Key), Escape(variable.Value)))],
            agent.KeepAlive,
            agent.ExitTimeout,
            agent.Log is { } log ? Escape(log.Value) : null);

        return new AgentUnit($"{agent.Label.Value}.plist", UnitTemplate.Fill(Plist.Value, unit));
    }

    /// <inheritdoc />
    public async Task<AgentOutcome> Plan(AgentDefinition agent, CancellationToken ct = default)
    {
        var unit = Render(agent);
        var installed = await agents.Directory.GetFile(unit.FileName).ReadAllTextIfExists(ct);
        if (!await IsLoaded(agent.Label, ct))
        {
            return AgentOutcome.Installed;
        }

        return installed == unit.Content ? AgentOutcome.Unchanged : AgentOutcome.Restarted;
    }

    /// <inheritdoc />
    public async Task<AgentOutcome> Converge(AgentDefinition agent, CancellationToken ct = default)
    {
        var unit = Render(agent);
        var file = agents.Directory.GetFile(unit.FileName);
        var installed = await file.ReadAllTextIfExists(ct);
        var loaded = await IsLoaded(agent.Label, ct);

        if (installed == unit.Content && loaded)
        {
            return AgentOutcome.Unchanged;
        }

        if (installed != unit.Content)
        {
            // Atomic: the supervisor may read the unit at any moment, and must never see half of it.
            await file.WriteAllText(unit.Content, mode: UnitPermissions.Owner, cancellationToken: ct);
        }

        if (!loaded)
        {
            await Bootstrap(file, ct);
            return AgentOutcome.Installed;
        }

        await Restart(agent, file, ct);
        return AgentOutcome.Restarted;
    }

    /// <inheritdoc />
    public async Task<bool> IsInstalled(string unit, CancellationToken ct = default) =>
        agents.Directory.GetFile($"{unit}.plist").Exists || await IsLoaded(unit, ct);

    /// <inheritdoc />
    public async Task Retire(string unit, CancellationToken ct = default)
    {
        if (await IsLoaded(unit, ct))
        {
            await commands.Run(Launchctl("bootout", $"{(await Domain(ct)).Value}/{unit}").ThrowOnError(), ct);
        }

        // The file too: launchd loads every plist in the directory at the next login, so a unit
        // that was only unloaded would be back after a reboot.
        agents.Directory.GetFile($"{unit}.plist").Delete();
        log.Status($"Retired {unit}.");
    }

    /// <summary>
    /// Whether launchd is running this label.
    /// </summary>
    private Task<bool> IsLoaded(AgentLabel label, CancellationToken ct) => IsLoaded(label.Value, ct);

    private async Task<bool> IsLoaded(string label, CancellationToken ct)
    {
        var result = await commands.Run(Launchctl("list").QuietOutput().ThrowOnError(), ct);
        return result.StandardOutput
            .Split('\n')
            .Any(line => line.Split('\t').LastOrDefault()?.Trim() == label);
    }

    /// <summary>
    /// The domain this process can bootstrap into, asked of the node once. .NET exposes no
    /// getuid, and the generated P/Invoke that would wants unsafe code turned on for the whole
    /// project — a large setting for one number the shell already knows.
    /// </summary>
    private async Task<UserDomain> Domain(CancellationToken ct)
    {
        if (_domain is { } known)
        {
            return known;
        }

        var result = await commands.Run(Command.Create("id").WithArguments("-u").QuietOutput().ThrowOnError(), ct);
        _domain = UserDomain.ForUser(result.StandardOutput);
        return _domain;
    }

    private async Task Bootstrap(IFile file, CancellationToken ct) =>
        await commands.Run(Launchctl("bootstrap", (await Domain(ct)).Value, file.AbsolutePath).ThrowOnError(), ct);

    private async Task Restart(AgentDefinition agent, IFile file, CancellationToken ct)
    {
        var target = (await Domain(ct)).Target(agent.Label);
        if (agent.Restart == AgentRestart.Signal)
        {
            await commands.Run(Launchctl("kill", "SIGTERM", target).ThrowOnError(), ct);
            log.Detail($"Signalled {agent.Label.Value}; it reads the new unit when it next starts.");
            return;
        }

        // A supervisor reads a unit when it loads it, so a changed unit only takes effect
        // across an unload.
        await commands.Run(Launchctl("bootout", target).ThrowOnError(), ct);
        await Bootstrap(file, ct);
    }

    private static Command Launchctl(params string[] arguments) => Command.Create("launchctl").WithArguments(arguments);

    /// <summary>
    /// The unit template, parsed once.
    /// </summary>
    private static readonly Lazy<Template> Plist = new(() => UnitTemplate.Load($"{typeof(LaunchdSupervisor).Namespace}.launchd.plist.sbn"));

    /// <summary>
    /// Escaped here rather than in the template, so the template cannot forget: everything it
    /// is handed is already safe to sit between tags.
    /// </summary>
    private static string Escape(string value) => SecurityElement.Escape(value) ?? value;
}
