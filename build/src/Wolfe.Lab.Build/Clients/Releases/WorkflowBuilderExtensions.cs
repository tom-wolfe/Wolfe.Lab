using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// Registers the release domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the name a component is released under, the installer that puts it there, and
        /// the installer's rehearsal.
        /// </summary>
        /// <param name="name">The name the component's <c>ritten.json</c> declares.</param>
        public IWorkflowBuilder AddReleases(string name)
        {
            builder.AddCommandRunner();
            builder.Services.AddSingleton(new ReleaseName(name));
            builder.Services.TryAddSingleton<IReleaseInstaller, RsyncInstaller>();
            builder.Decorators.Replace<IReleaseInstaller, DryRunInstaller>();
            return builder;
        }
    }
}
