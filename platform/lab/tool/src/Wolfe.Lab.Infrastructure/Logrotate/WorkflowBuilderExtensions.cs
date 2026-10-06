using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Infrastructure.Logrotate;

/// <summary>
/// Registers logrotate.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="ILogrotate"/> over the node's logrotate, and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddLogrotate()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<ILogrotate, LogrotateClient>();
            builder.Decorators.Replace<ILogrotate, DryRunLogrotate>();
            return builder;
        }
    }
}
