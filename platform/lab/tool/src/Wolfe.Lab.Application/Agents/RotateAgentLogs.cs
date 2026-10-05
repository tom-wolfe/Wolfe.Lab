using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Logrotate;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// Keeps the logs the component's agents write on this node to a size, so a log is history
/// rather than a disk filling up.
/// </summary>
/// <remarks>
/// The component's own configuration, written beside its state under
/// <c>${LAB_ROOT}/.logrotate</c>, rotates exactly the logs it declares
/// (<see cref="LogrotateConfiguration"/>); logrotate keeps when it last did in the state file, so
/// running it more often than a log grows changes nothing. A rehearsal writes its configuration
/// to the run's scratch instead, and logrotate only says what it would do.
/// </remarks>
[Step("rotate agent logs", StepKind.Publish)]
internal sealed class RotateAgentLogs(ILogrotate logrotate, IFileSystem fileSystem, IOptions<LabDirectories> options, WorkflowJob job, IWorkflowLog log)
{
    internal const string Directory = ".logrotate";

    public async Task<StepResult> Run(AgentDeclarations declarations, DeploymentUnit unit, CancellationToken ct = default)
    {
        if (!unit.ByWorkflow(job.WorkflowName).TryGetValue(out var component, out var unowned))
        {
            return StepResult.Failed(unowned);
        }

        var directories = options.Value;
        var logs = declarations.Agents.Values
            .Select(agent => ResolveAgents.Expand(agent, directories).Log)
            .OfType<HostPath>()
            .Select(path => path.File)
            .ToList();
        if (logs.Count == 0)
        {
            log.Detail($"{component} writes no log files.");
            return StepResult.Successful;
        }

        var rotations = directories.Root.GetDirectory(Directory);
        var configuration = (job.DryRun ? fileSystem.Temp.GetDirectory(Directory) : rotations).GetFile($"{component.QualifiedName}.conf");
        await configuration.WriteAllText(LogrotateConfiguration.For(logs), cancellationToken: ct);
        await logrotate.Rotate(configuration, rotations.GetFile($"{component.QualifiedName}.state"), ct);

        var names = string.Join(", ", logs.Select(file => file.AbsolutePath));
        if (job.DryRun)
        {
            log.Skipped($"Would rotate {names}, past {LogrotateConfiguration.Size}, keeping {LogrotateConfiguration.Generations}.");
        }
        else
        {
            log.Status($"Rotated {names} as they need it.");
        }

        return StepResult.Successful;
    }
}
