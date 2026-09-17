using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// Registers the secrets client.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="ISecrets"/>.
        /// </summary>
        public IWorkflowBuilder AddSecrets()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<ISecrets, OnePasswordSecrets>();
            return builder;
        }
    }
}
