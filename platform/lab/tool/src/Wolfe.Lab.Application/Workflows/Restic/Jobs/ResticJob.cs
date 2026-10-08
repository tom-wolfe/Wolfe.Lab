using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Infrastructure.Restic;

namespace Wolfe.Lab.Application.Workflows.Restic.Jobs;

/// <summary>
/// What every restic job registers: the client.
/// </summary>
internal abstract class ResticJob : LabJob<DeclaredSettings>
{
    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddRestic();
    }
}
