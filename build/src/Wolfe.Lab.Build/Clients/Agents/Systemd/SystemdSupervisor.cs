using System.Globalization;
using System.Text;
using Scriban;

namespace Wolfe.Lab.Build.Clients.Agents.Systemd;

/// <summary>
/// systemd, as the user's manager, through <c>systemctl --user</c>. Units are written to the
/// user's unit directory, enabled so they start at boot, and started.
/// </summary>
/// <remarks>
/// A user manager runs units only while the user has a session unless lingering is on
/// (<c>loginctl enable-linger</c>), which a node's bootstrap turns on once.
/// <para>
/// Unlike launchd, systemd re-reads a unit on <c>daemon-reload</c>, so a changed unit is read
/// on the process's next start however it is restarted — which is what makes
/// <see cref="AgentRestart.Signal"/> take effect here.
/// </para>
/// </remarks>
internal sealed class SystemdSupervisor(AgentDirectory agents, ICommandRunner commands, IWorkflowLog log) : IServiceSupervisor
{
    /// <inheritdoc />
    public AgentUnit Render(AgentDefinition agent)
    {
        var unit = new SystemdUnit(
            Specifiers(agent.Label.Value),
            agent.Program.Value,
            agent.ProgramStamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            string.Join(' ', agent.Arguments.Prepend(agent.Program.Value).Select(ExecWord)),
            agent.WorkingDirectory is { } workingDirectory ? Specifiers(workingDirectory.Value) : null,
            // Ordered, because the render is compared as text: a dictionary that came out of
            // JSON in a different order must not read as a changed agent.
            [.. agent.Environment
                .OrderBy(variable => variable.Key, StringComparer.Ordinal)
                .Select(variable => new UnitVariable(variable.Key, Quoted(Specifiers($"{variable.Key}={variable.Value}"))))],
            agent.KeepAlive,
            agent.ExitTimeout,
            agent.Log is { } path ? Specifiers(path.Value) : null);

        return new AgentUnit(UnitName(agent.Label), UnitTemplate.Fill(Service.Value, unit));
    }

    /// <inheritdoc />
    public async Task<AgentOutcome> Plan(AgentDefinition agent, CancellationToken ct = default)
    {
        var unit = Render(agent);
        var installed = await agents.Directory.GetFile(unit.FileName).ReadAllTextIfExists(ct);
        if (!await IsRunning(agent.Label, ct))
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
        var running = await IsRunning(agent.Label, ct);
        if (installed == unit.Content && running)
        {
            return AgentOutcome.Unchanged;
        }

        if (installed != unit.Content)
        {
            // Atomic: the manager may read the unit at any moment, and must never see half of it.
            await file.WriteAllText(unit.Content, cancellationToken: ct);
            await commands.Run(Systemctl("daemon-reload").ThrowOnError(), ct);
        }

        if (!running)
        {
            // restart rather than start: it also recovers a unit that is loaded but failed.
            await commands.Run(Systemctl("enable", unit.FileName).ThrowOnError(), ct);
            await commands.Run(Systemctl("restart", unit.FileName).ThrowOnError(), ct);
            return AgentOutcome.Installed;
        }

        await Restart(agent, unit, ct);
        return AgentOutcome.Restarted;
    }

    /// <summary>
    /// Whether systemd is running this unit and will start it at boot — the two things
    /// launchd's "loaded" means together.
    /// </summary>
    private async Task<bool> IsRunning(AgentLabel label, CancellationToken ct)
    {
        var result = await commands.Run(
            Systemctl("show", UnitName(label), "--property=ActiveState", "--property=UnitFileState").QuietOutput().ThrowOnError(),
            ct);
        var properties = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0], pair => pair[1]);

        return properties.GetValueOrDefault("ActiveState") is "active" or "activating" or "reloading"
            && properties.GetValueOrDefault("UnitFileState") == "enabled";
    }

    private async Task Restart(AgentDefinition agent, AgentUnit unit, CancellationToken ct)
    {
        if (agent.Restart == AgentRestart.Signal)
        {
            // The main process only, by its pid. `systemctl kill` signals the whole control
            // group by default — which, for the runner, includes the job doing this converge.
            var pid = await commands.Run(
                Systemctl("show", unit.FileName, "--property=MainPID", "--value").QuietOutput().ThrowOnError(),
                ct);
            await commands.Run(Command.Create("kill").WithArguments("-TERM", pid.StandardOutput.Trim()).ThrowOnError(), ct);
            log.Detail($"Signalled {agent.Label.Value}; systemd starts it again with the new unit.");
            return;
        }

        await commands.Run(Systemctl("restart", unit.FileName).ThrowOnError(), ct);
    }

    private static Command Systemctl(params string[] arguments) =>
        Command.Create("systemctl").WithArguments(["--user", .. arguments]);

    private static string UnitName(AgentLabel label) => $"{label.Value}.service";

    /// <summary>
    /// The unit template, parsed once.
    /// </summary>
    private static readonly Lazy<Template> Service = new(() => UnitTemplate.Load($"{typeof(SystemdSupervisor).Namespace}.systemd.service.sbn"));

    /// <summary>
    /// One word of <c>ExecStart=</c>: quoted, and with <c>$</c> doubled, because systemd
    /// expands <c>$NAME</c> there and nowhere else a unit's values go.
    /// </summary>
    private static string ExecWord(string value) => Quoted(Specifiers(value).Replace("$", "$$", StringComparison.Ordinal));

    /// <summary>
    /// A value with systemd's <c>%</c> specifiers made literal. Every setting expands them.
    /// </summary>
    private static string Specifiers(string value) => value.Replace("%", "%%", StringComparison.Ordinal);

    /// <summary>
    /// A double-quoted word, with the escapes systemd's quoting reads: a backslash, a quote, and
    /// the control characters a unit line cannot hold.
    /// </summary>
    internal static string Quoted(string value)
    {
        var quoted = new StringBuilder("\"", value.Length + 2);
        foreach (var c in value)
        {
            quoted.Append(c switch
            {
                '\\' => @"\\",
                '"' => "\\\"",
                '\n' => @"\n",
                '\t' => @"\t",
                '\r' => @"\r",
                _ => c.ToString()
            });
        }

        return quoted.Append('"').ToString();
    }
}
