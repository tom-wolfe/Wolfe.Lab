using Microsoft.Extensions.DependencyInjection;

namespace Wolfe.Lab.Build.Alerts;

/// <summary>
/// Registers the alert transport.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IAlerts"/> over Pushover, and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddAlerts()
        {
            builder.Services.AddHttpClient<IAlerts, PushoverAlerts>();
            builder.Decorators.Replace<IAlerts, DryRunAlerts>();
            return builder;
        }
    }
}
