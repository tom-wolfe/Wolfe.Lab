using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// Registers the node's service supervisor.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IServiceSupervisor"/> over launchd, and its rehearsal. When the
        /// primary node becomes a Linux box this is the one registration that changes.
        /// </summary>
        public IWorkflowBuilder AddAgents()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton(AgentDirectory.Default);
            builder.Services.TryAddSingleton<LaunchdSupervisor>();
            builder.Services.TryAddSingleton<IServiceSupervisor>(services => services.GetRequiredService<LaunchdSupervisor>());
            builder.Decorators.Replace<IServiceSupervisor, DryRunSupervisor>();
            return builder;
        }
    }
}
