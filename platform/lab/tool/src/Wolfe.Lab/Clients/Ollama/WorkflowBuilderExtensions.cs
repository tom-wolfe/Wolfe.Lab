using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Clients.Resilience;

namespace Wolfe.Lab.Clients.Ollama;

/// <summary>
/// Registers the model server client.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IOllama"/> over the ollama binary, and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddOllama()
        {
            builder
                .AddCommandRunner()
                .AddPolling(OllamaClient.Serving, "Ollama:Serving");
            builder.Services.TryAddSingleton<OllamaClient>();
            builder.Services.TryAddSingleton<IOllama>(services => services.GetRequiredService<OllamaClient>());
            builder.Decorators.Replace<IOllama, DryRunOllama>();
            return builder;
        }
    }
}
