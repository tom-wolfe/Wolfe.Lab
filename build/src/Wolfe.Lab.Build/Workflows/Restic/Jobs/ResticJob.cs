using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Restic;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Workflows.Restic.Models;

namespace Wolfe.Lab.Build.Workflows.Restic.Jobs;

/// <summary>
/// What every restic job registers: the client, and the drive the local repository lives on.
/// </summary>
internal abstract class ResticJob : LabJob<ResticSettings>
{
    protected override void Configure(IWorkflowBuilder builder, ResticSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddRestic();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(v => v.Directory)]));
    }
}
