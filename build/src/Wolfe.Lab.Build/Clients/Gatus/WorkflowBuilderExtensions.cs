using Microsoft.Extensions.DependencyInjection;

namespace Wolfe.Lab.Build.Clients.Gatus;

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
            // The same patience the probe had as a curl: a status page that takes longer than
            // this to answer is down for every purpose that matters.
            builder.Services.AddHttpClient<IGatus, GatusClient>(client => client.Timeout = TimeSpan.FromSeconds(20));
            return builder;
        }
    }
}
