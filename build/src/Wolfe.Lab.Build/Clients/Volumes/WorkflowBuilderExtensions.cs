using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Clients.Volumes;

/// <summary>
/// Registers the volumes domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the volumes a component declares, for the guard that checks them.
        /// </summary>
        /// <remarks>
        /// An unmounted <c>/Volumes</c> path on macOS is an ordinary empty directory on the
        /// internal disk, and a stack that starts against one runs happily on nothing. The
        /// sentinel file the guard looks for only exists on the real drive.
        /// </remarks>
        public IWorkflowBuilder AddVolumes(IReadOnlyList<HostPath> volumes)
        {
            builder.Services.AddSingleton(new RequiredVolumes([.. volumes.Select(volume => volume.Directory)]));
            return builder;
        }
    }
}
