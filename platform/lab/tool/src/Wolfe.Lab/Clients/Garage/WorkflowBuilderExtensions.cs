using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Clients.Resilience;

namespace Wolfe.Lab.Clients.Garage;

/// <summary>
/// Registers the Garage client and its rehearsal.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the Garage client and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddGarage()
        {
            builder.AddCommandRunner()
                .AddPolling(GarageClient.Answering, "Garage:Answering");
            builder.Services.TryAddSingleton<GarageClient>();
            builder.Services.TryAddSingleton<IGarage>(services => services.GetRequiredService<GarageClient>());
            builder.Decorators.Replace<IGarage, DryRunGarage>();
            return builder;
        }
    }
}
