using Microsoft.Extensions.DependencyInjection;

namespace Wolfe.Lab.Infrastructure.Alerts;

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
            builder.AddLabConfiguration();
            builder.Services.AddOptions<AlertsOptions>().BindConfiguration("Alerts")
                .Validate(options => options.Endpoint is not null, "'Alerts:Endpoint' is not set in appsettings.json.");
            builder.Services.AddHttpClient<IAlerts, PushoverAlerts>();
            builder.Decorators.Replace<IAlerts, DryRunAlerts>();
            return builder;
        }
    }
}
