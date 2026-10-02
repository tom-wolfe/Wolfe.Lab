using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Infrastructure.Resilience;

namespace Wolfe.Lab.Infrastructure.Gatus;

/// <summary>
/// Registers the Gatus client.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the Gatus client. No rehearsal twin: a probe is a read, and rehearsing it means reading.
        /// </summary>
        public IWorkflowBuilder AddGatus()
        {
            builder.Services
                .AddHttpClient<IGatus, GatusClient>()
                .AddConfiguredResilience("Gatus:Http");
            return builder;
        }
    }
}
