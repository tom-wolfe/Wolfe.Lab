using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Restic;

/// <summary>
/// Registers the restic client.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IRestic"/>. The rehearsal is a replacement rather than a wrapper: it
        /// never writes a snapshot, it lists one.
        /// </summary>
        public IWorkflowBuilder AddRestic()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IRestic, ResticClient>();
            builder.Decorators.Replace<IRestic, DryRunRestic>();
            return builder;
        }
    }
}
