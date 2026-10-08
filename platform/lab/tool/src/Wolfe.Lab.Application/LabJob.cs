using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Git;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Infrastructure;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application;

/// <summary>
/// The base for every lab job: the clients any step may reach for, registered once.
/// </summary>
/// <typeparam name="TOptions">The shape of the service's <c>ritten.json</c>.</typeparam>
public abstract class LabJob<TOptions> : Job<TOptions> where TOptions : WorkflowSettings
{
    /// <summary>
    /// Whether the job needs a <c>ritten.json</c>: only to read settings its component does not yet declare.
    /// </summary>
    public override bool RequiresProject => typeof(TOptions) != typeof(DeclaredSettings);

    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, TOptions options)
    {
        builder.AddCommandRunner();
        builder.AddGit();
        builder.AddLabConfiguration();
        builder.AddLabRoots();
        builder.AddLabNode();
        builder.Services.TryAddSingleton<AgentResolver>();
        builder.Services.TryAddSingleton<EnvironmentPath>();
        builder.Services.TryAddSingleton<DeclaredComponents>();
    }
}
