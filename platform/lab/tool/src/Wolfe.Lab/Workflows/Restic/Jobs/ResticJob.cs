using Wolfe.Lab.Clients.Restic;
using Wolfe.Lab.Clients.Volumes;
using Wolfe.Lab.Workflows.Restic.Models;

namespace Wolfe.Lab.Workflows.Restic.Jobs;

/// <summary>
/// What every restic job registers: the client, and the drive the local repository lives on.
/// </summary>
internal abstract class ResticJob : LabJob<ResticOptions>
{
    protected override void Configure(IWorkflowBuilder builder, ResticOptions options)
    {
        base.Configure(builder, options);
        builder.AddRestic().AddVolumes(options.Volumes);
    }
}
