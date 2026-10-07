using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Agents.Jobs;

/// <summary>
/// Converges one node's agents.
/// </summary>
internal sealed class DeployJob : LabJob<DeclaredSettings>
{
    public override string Name => "deploy";

    public override string Description => "Installs the deployment and converges this node's agents, retiring any they supersede.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveAgentDeclarations>(),
        Step.FromType<InstallAgentPackages>(),
        Step.FromType<ResolveAgents>(),
        Step.FromType<GateApproval>(),
        Step.FromType<InstallDeployment>(),
        Step.FromType<ConvergeAgents>(),
        Step.FromType<DeclareAgentLogs>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddPackages().AddAgents().AddInstaller();
    }
}
