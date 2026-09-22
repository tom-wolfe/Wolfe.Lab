using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Deploy;

/// <summary>
/// Registers the slice installer.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="ISliceInstaller"/>. The rehearsal is a replacement rather than a
        /// wrapper: it never installs, it lists.
        /// </summary>
        public IWorkflowBuilder AddSliceInstaller()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<ISliceInstaller, RsyncInstaller>();
            builder.Decorators.Replace<ISliceInstaller, DryRunInstaller>();
            return builder;
        }
    }
}
