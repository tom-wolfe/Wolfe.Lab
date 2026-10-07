using Ritten.Docker;
using Wolfe.Lab.Application.Workflows.Backup.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Backups;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Backup.Steps;

/// <summary>
/// Works out what the backup snapshots, and what it is part of: the stack installed on this
/// node that is stopped while it is taken, and the container whose image tags it.
/// </summary>
[Step("resolve backup", StepKind.Work)]
internal sealed class ResolveBackupPlan(IDocker docker, IOptions<LabDirectories> options, IWorkflowLog log)
{
    public async Task<StepResult<BackupPlan>> Run(ServiceCatalog catalog, DeploymentUnit unit, CancellationToken ct = default)
    {
        if (!unit.ByWorkflow(WorkflowName.Backup).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var backup = (BackupComponent)declared;
        IDirectory? stack = null;
        string? container = null;
        if (backup.PartOf is not null)
        {
            if (backup.Host is not DockerComponent host || catalog.DeploymentUnitAt(host.Directory)?.Value is not { } deployed)
            {
                return new Error($"{backup} is part of {backup.Host?.ToString() ?? backup.PartOf.ToString()}, which is no compose stack's: only a Docker component is stopped for a snapshot.");
            }

            stack = options.Value.DeployedTo(deployed);
            if (!(await docker.ComposeConfig(stack, ct: ct)).TryGetValue(out var project, out var unreadable))
            {
                return StepResult.Failed(ComposeErrors.Unreadable(stack, unreadable));
            }

            if (project.Services.FirstOrDefault(service => service.Name == host.ComposeService.Value) is not { } service)
            {
                return new Error($"{stack.AbsolutePath} runs no {host.ComposeService} service for {host}.");
            }

            container = service.Container;
        }

        var plan = new BackupPlan(
            unit.Service.Name.Value,
            [.. backup.Paths.Select(path => path.Directory)],
            [.. backup.Excludes.Select(path => path.Value)],
            [.. backup.Verify.Select(path => path.Value)],
            backup.Warm ? null : stack,
            backup.Warm ? null : container,
            container);
        log.Detail(plan switch
        {
            { Stack: { } stopped } => $"Snapshots {Paths(plan)} of {unit.Service}, stopping {stopped.AbsolutePath} for it.",
            { Image: { } running } => $"Snapshots {Paths(plan)} of {unit.Service} while {running} runs.",
            _ => $"Snapshots {Paths(plan)} of {unit.Service}; nothing runs from them."
        });
        return plan;
    }

    private static string Paths(BackupPlan plan) => string.Join(", ", plan.Paths.Select(path => path.AbsolutePath));
}
