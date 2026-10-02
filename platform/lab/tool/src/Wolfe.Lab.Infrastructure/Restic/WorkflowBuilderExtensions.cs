using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Infrastructure.Restic;

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
            // restic's version is the repository's to pin (.config/lab-tools.json): every node
            // writes the same repositories, so every node runs the same one.
            builder.AddCommandRunner().AddTools("restic");
            builder.Services.TryAddSingleton<ResticClient>();
            builder.Services.TryAddSingleton<IRestic>(s => s.GetRequiredService<ResticClient>());
            builder.Decorators.Replace<IRestic, DryRunRestic>();
            return builder;
        }
    }
}
