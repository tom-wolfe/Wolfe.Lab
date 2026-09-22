
using Wolfe.Lab.Build.Workflows.Docker.Jobs;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker;

/// <summary>
/// An application that ships as a container: <c>"workflow": "dotnet-service"</c>.
/// </summary>
/// <remarks>
/// Check is .NET's; deploy is the docker deploy, unchanged — the same job the plain
/// <c>docker</c> workflow runs, because building an image and converging a stack does not
/// become a different act when the image came from source in the same directory.
/// </remarks>
public sealed class DotNetServiceWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "dotnet-service";

    /// <inheritdoc />
    public string Label => "dotnet-service";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new DotNetCheckJob(), new DeployJob<DotNetServiceSettings>()];
}
