using Wolfe.Lab.Application.Workflows.Restic.Models;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Application.Workflows.Restic.Jobs;

/// <summary>
/// What every restic job registers: the client.
/// </summary>
internal abstract class ResticJob : LabJob<ResticOptions>
{
    protected override void Configure(IWorkflowBuilder builder, ResticOptions options)
    {
        base.Configure(builder, options);
        builder.AddRestic();
    }
}
