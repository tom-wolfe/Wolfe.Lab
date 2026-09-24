using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Build.Clients.Agents.Launchd;
using Wolfe.Lab.Build.Clients.Agents.Systemd;

namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// Registers the node's service supervisor.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IServiceSupervisor"/> for this node's operating system.
        /// </summary>
        public IWorkflowBuilder AddAgents()
        {
            builder.AddCommandRunner();
            if (OperatingSystem.IsMacOS())
            {
                builder.Services.TryAddSingleton(AgentDirectory.Launchd);
                builder.Services.TryAddSingleton<IServiceSupervisor, LaunchdSupervisor>();

            }
            else if (OperatingSystem.IsLinux())
            {
                builder.Services.TryAddSingleton(AgentDirectory.Systemd);
                builder.Services.TryAddSingleton<IServiceSupervisor, SystemdSupervisor>();
            }
            else
            {
                throw new PlatformNotSupportedException("The lab's agents run under launchd or systemd: macOS or Linux.");
            }

            builder.Decorators.Replace<IServiceSupervisor, DryRunSupervisor>();
            builder.Services.TryAddSingleton<PlatformSupervisor>();
            return builder;
        }
    }
}
