using Microsoft.Extensions.DependencyInjection;

namespace Wolfe.Lab.Build.Clients.Heartbeat;

/// <summary>
/// Registers the dead man's switch transport.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IHeartbeat"/> over healthchecks.io, and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddHeartbeat()
        {
            builder.Services.AddHttpClient<IHeartbeat, HealthchecksHeartbeat>();
            builder.Decorators.Replace<IHeartbeat, DryRunHeartbeat>();
            return builder;
        }
    }
}
