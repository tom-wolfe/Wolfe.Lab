using System.Globalization;
using System.Security;
using Scriban;
using Scriban.Runtime;

namespace Wolfe.Lab.Build.Agents;

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

        return new AgentUnit($"{agent.Label.Value}.plist", Fill(Plist.Value, unit));
    }

    /// <inheritdoc />
    public async Task<AgentOutcome> Plan(AgentDefinition agent, CancellationToken ct = default)
    {
        var unit = Render(agent);
        var installed = Read(agents.Directory.GetFile(unit.FileName));
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
        var installed = Read(file);
        var loaded = await IsLoaded(agent.Label, ct);

        if (installed == unit.Content && loaded)
        {
            return AgentOutcome.Unchanged;
        }

        if (installed != unit.Content)
        {
            agents.Directory.Create();
            Write(file, unit.Content);
        }

        if (!loaded)
        {
            await Bootstrap(file, ct);
            return AgentOutcome.Installed;
        }

        await Restart(agent, file, ct);
        return AgentOutcome.Restarted;
    }

    /// <summary>
    /// Whether launchd is running this label.
    /// </summary>
    private async Task<bool> IsLoaded(AgentLabel label, CancellationToken ct)
    {
        var result = await commands.Run(Launchctl("list").QuietOutput().ThrowOnError(), ct);
        return result.StandardOutput
            .Split('\n')
            .Any(line => line.Split('\t').LastOrDefault()?.Trim() == label.Value);
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

    private static string? Read(IFile file)
    {
        if (!file.Exists)
        {
            return null;
        }

        using var reader = new StreamReader(file.OpenRead());
        return reader.ReadToEnd();
    }

    private static void Write(IFile file, string content)
    {
        // Deleted first: a shorter unit written over a longer one would otherwise keep the
        // tail of the old.
        if (file.Exists)
        {
            file.Delete();
        }

        using var writer = new StreamWriter(file.OpenWrite());
        writer.Write(content);
    }

    /// <summary>
    /// The unit template, parsed once. A template that does not parse is a packaging fault
    /// rather than anything a node did, so it is thrown rather than reported.
    /// </summary>
    private static readonly Lazy<Template> Plist = new(() => Load("launchd.plist.sbn"));

    private static Template Load(string name)
    {
        var resource = $"{typeof(LaunchdSupervisor).Namespace}.Templates.{name}";
        using var stream = typeof(LaunchdSupervisor).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"{resource} is not in the assembly: the template was not embedded.");

        using var reader = new StreamReader(stream);
        var template = Template.Parse(reader.ReadToEnd());
        return template.HasErrors
            ? throw new InvalidOperationException($"{name} does not parse: {string.Join("; ", template.Messages)}")
            : template;
    }

    /// <summary>
    /// Renders a model through a template under its own member names, so the template reads as
    /// the record beside it rather than in a second naming convention.
    /// </summary>
    private static string Fill(Template template, object model)
    {
        var globals = new ScriptObject();
        globals.Import(model, renamer: member => member.Name);

        var context = new TemplateContext { MemberRenamer = member => member.Name };
        context.PushGlobal(globals);

        return template.Render(context).TrimEnd('\n') + '\n';
    }

    /// <summary>
    /// Escaped here rather than in the template, so the template cannot forget: everything it
    /// is handed is already safe to sit between tags.
    /// </summary>
    private static string Escape(string value) => SecurityElement.Escape(value) ?? value;
}
