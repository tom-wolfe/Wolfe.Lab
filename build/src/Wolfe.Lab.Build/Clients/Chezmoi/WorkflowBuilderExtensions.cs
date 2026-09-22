using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Clients.Chezmoi;

/// <summary>
/// Registers the chezmoi client and its rehearsal.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the chezmoi client and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddChezmoi()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<ChezmoiClient>();
            builder.Services.TryAddSingleton<IChezmoi>(services => services.GetRequiredService<ChezmoiClient>());
            builder.Decorators.Replace<IChezmoi, DryRunChezmoi>();
            return builder;
        }
    }
}
