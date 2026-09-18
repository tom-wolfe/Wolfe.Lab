using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Immich.Jobs;

/// <summary>
/// Installs the slice on the node and converges its stack.
/// </summary>
internal sealed class DeployJob : ImmichJob
{
    public override string Name => "deploy";

    public override string Description => "Installs the slice on the node and converges its compose stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<InstallSlice>(),
        Step.FromType<ResolveComposeSecrets>(),
        Step.FromType<ApprovalGate>(),
        Step.FromType<ComposeUp>()
    ];

    public override JobKind Kind => JobKind.Deploy;
}
