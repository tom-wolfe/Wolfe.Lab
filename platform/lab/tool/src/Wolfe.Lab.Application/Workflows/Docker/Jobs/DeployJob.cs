using Ritten.Docker;
using Ritten.Docker.Steps;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Volumes;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Docker.Jobs;

/// <summary>
/// Installs a compose component on the node and converges its stack.
/// </summary>
/// <remarks>
/// The component is installed rather than run from the checkout because containers bind-mount
/// files out of it and go on reading them after the job that deployed them is gone. Images are
/// built from the checkout first, because a build context is source and the installer does not
/// carry source onto the node.
/// </remarks>
internal sealed class DeployJob<TOptions> : LabJob<TOptions> where TOptions : DockerComponentOptions
{
    public override string Name => "deploy";

    public override string Description => "Builds the component's images, installs it on the node and converges its stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<ResolveArtifacts>(),
        Step.FromType<PublishArtifacts>(),
        Step.FromType<BuildImages>(),
        Step.FromType<ResolveComposeSecrets>(),
        Step.FromType<GateApproval>(),
        Step.FromType<LabelServices>(),
        Step.FromType<ConvergeStack>(),
        Step.FromType<DeclareMetrics>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<TOptions> options) => options
        .Require(s => s.Images.All(image => image.ToImage() is not null), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, TOptions options)
    {
        base.Configure(builder, options);

        // The deployment is the first artifact: where the stack runs from.
        builder.AddDocker([.. options.Images.Select(image => image.ToImage()).OfType<DockerImage>()]).AddArtifacts(options.Artifacts, installsUnit: true);
    }
}
