using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfe.Lab.Build.Clients.Resilience;

namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// Registers the package domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the installer that puts GitHub releases on the node, and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddPackages()
        {
            builder.AddCommandRunner().AddBuildReporting().AddLabConfiguration();
            builder.Services.AddOptions<GithubOptions>().BindConfiguration("Packages")
                .Validate(options => options.Releases is not null && options.Api is not null, "'Packages:Releases' and 'Packages:Api' must both be set in appsettings.json.");
            builder.Services
                .AddHttpClient(GithubPackageInstaller.Client)
                .AddConfiguredResilience("Packages:Http");
            builder.Services.TryAddSingleton<IPackageInstaller, GithubPackageInstaller>();
            builder.Decorators.Replace<IPackageInstaller, DryRunPackageInstaller>();
            return builder;
        }

        /// <summary>
        /// Adds the tools a job runs, pinned by <c>.config/lab-tools.json</c>; the job lists
        /// <see cref="Steps.EnsureTools"/> before the first step that runs one.
        /// </summary>
        /// <param name="names">The commands.</param>
        public IWorkflowBuilder AddTools(params string[] names)
        {
            builder.AddPackages();
            // Several domains a job uses may each name one: they add up, rather than the last one winning.
            var existing = builder.Services.FirstOrDefault(service => service.ServiceType == typeof(RequiredTools));
            var required = existing?.ImplementationInstance is RequiredTools earlier ? earlier.Names : [];
            if (existing is not null)
            {
                builder.Services.Remove(existing);
            }

            builder.Services.AddSingleton(new RequiredTools([.. required.Union(names, StringComparer.Ordinal)]));
            return builder;
        }
    }
}
