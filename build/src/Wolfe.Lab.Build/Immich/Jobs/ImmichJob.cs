using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Services;
using Wolfe.Lab.Build.Immich.Models;

namespace Wolfe.Lab.Build.Immich.Jobs;

/// <summary>
/// What every immich job registers: the deploy clients and the slice's required volumes.
/// </summary>
internal abstract class ImmichJob : LabJob<ImmichSettings>
{
    protected override void Configure(IWorkflowBuilder builder, ImmichSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker().AddSliceInstaller();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(v => v.Directory)]));
    }
}
