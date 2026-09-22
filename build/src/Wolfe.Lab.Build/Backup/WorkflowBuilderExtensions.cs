using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Backup;

/// <summary>
/// Registers the state directory client.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IStateDirectories"/> and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddStateDirectories()
        {
            builder.Services.TryAddSingleton<IStateDirectories, StateDirectories>();
            builder.Decorators.Replace<IStateDirectories, DryRunStateDirectories>();
            return builder;
        }
    }
}
