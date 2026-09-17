using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Build.Obsidian.Services;

namespace Wolfe.Lab.Build.Obsidian;

/// <summary>
/// Registers the Obsidian client.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IObsidian"/> and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddObsidian()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IObsidian, ObsidianClient>();
            builder.Decorators.Replace<IObsidian, DryRunObsidian>();
            return builder;
        }
    }
}
