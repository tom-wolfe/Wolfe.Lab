using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Wolfe.Lab.Infrastructure.Releases;

/// <summary>
/// Registers the release domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds where the lab installs and keeps state on this node, as <see cref="LabDirectories"/> options.
        /// </summary>
        public IWorkflowBuilder AddLabRoots()
        {
            builder.AddLabConfiguration();
            if (builder.Services.Any(service => service.ServiceType == typeof(IConfigureOptions<LabDirectories>)))
            {
                return builder;
            }

            builder.Services.AddOptions<LabDirectories>().Configure<IConfiguration>((roots, configuration) => roots.Configure(configuration));
            return builder;
        }

        /// <summary>
        /// Adds which node the lab is running on, as <see cref="LabNode"/> options.
        /// </summary>
        public IWorkflowBuilder AddLabNode()
        {
            builder.AddLabConfiguration();
            if (builder.Services.Any(service => service.ServiceType == typeof(IConfigureOptions<LabNode>)))
            {
                return builder;
            }

            builder.Services.AddOptions<LabNode>().Configure<IConfiguration>((node, configuration) => node.Configure(configuration));
            return builder;
        }

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

        /// <summary>
        /// Adds the artifacts a job publishes, and the installer that mirrors them onto the node.
        /// </summary>
        /// <param name="artifacts">The component's declarations, and any the workflow adds.</param>
        public IWorkflowBuilder AddArtifacts(IReadOnlyList<ArtifactOptions> artifacts)
        {
            builder.AddCommandRunner().AddBuildReporting();
            builder.Services.AddSingleton(new ArtifactDeclarations(artifacts));
            builder.Services.TryAddSingleton<IReleaseInstaller, RsyncInstaller>();
            builder.Decorators.Replace<IReleaseInstaller, DryRunInstaller>();
            return builder;
        }
    }
}
