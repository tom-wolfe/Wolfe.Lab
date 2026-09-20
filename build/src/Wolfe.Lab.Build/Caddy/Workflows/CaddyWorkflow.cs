using Wolfe.Lab.Build.Caddy.Jobs;

namespace Wolfe.Lab.Build.Caddy.Workflows;

/// <summary>
/// The front door: <c>"workflow": "caddy"</c>. A compose slice like any other, except that
/// deploying it also republishes every other slice's route — which is why it is not on the
/// generic <c>service</c> workflow.
/// </summary>
public sealed class CaddyWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "caddy";

    /// <inheritdoc />
    public string Label => "caddy";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DeployJob()];
}
