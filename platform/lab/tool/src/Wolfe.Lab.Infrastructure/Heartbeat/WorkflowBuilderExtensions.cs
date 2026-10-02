using Microsoft.Extensions.DependencyInjection;

namespace Wolfe.Lab.Infrastructure.Heartbeat;

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
            builder.AddLabConfiguration();
            builder.Services.AddOptions<HealthchecksOptions>().BindConfiguration("Heartbeat")
                .Validate(options => options.Endpoint is not null, "'Heartbeat:Endpoint' is not set in appsettings.json.");
            builder.Services.AddHttpClient<IHeartbeat, HealthchecksHeartbeat>();
            builder.Decorators.Replace<IHeartbeat, DryRunHeartbeat>();
            return builder;
        }
    }
}
