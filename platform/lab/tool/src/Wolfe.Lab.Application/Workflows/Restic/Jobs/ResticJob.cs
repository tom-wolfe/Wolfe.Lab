using Wolfe.Lab.Application.Workflows.Restic.Models;
using Wolfe.Lab.Infrastructure.Restic;
using Wolfe.Lab.Infrastructure.Volumes;

namespace Wolfe.Lab.Application.Workflows.Restic.Jobs;

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
