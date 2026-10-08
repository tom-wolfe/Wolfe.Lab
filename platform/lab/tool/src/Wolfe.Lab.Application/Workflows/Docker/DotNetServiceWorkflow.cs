
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Docker.Jobs;

namespace Wolfe.Lab.Application.Workflows.Docker;

/// <summary>
/// An application that ships as a container: <c>"workflow": "dotnet-service"</c>.
/// </summary>
/// <remarks>
/// Check is .NET's; deploy is the docker deploy, unchanged — the same job the plain
/// <c>docker</c> workflow runs, because building an image and converging a stack does not
/// become a different act when the image came from source in the same directory.
/// </remarks>
public sealed class DotNetServiceWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "dotnet-service";

    /// <inheritdoc />
    public override string Label => "dotnet-service";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new DotNetCheckJob(), new DeployJob<DeclaredSettings>()];
}
