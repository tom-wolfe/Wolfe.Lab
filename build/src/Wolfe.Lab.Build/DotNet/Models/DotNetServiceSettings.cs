using Wolfe.Lab.Build.Docker.Models;

namespace Wolfe.Lab.Build.DotNet.Models;

/// <summary>
/// The shape of a .NET service component's <c>ritten.json</c>:
/// <c>"workflow": "dotnet-service"</c>.
/// </summary>
/// <remarks>
/// One component, one directory: the source, the image it builds to, and the stack that runs
/// it. Splitting those across directories would only put a build context in one component and
/// a compose file in another, which is the coupling this shape exists to remove — and
/// "an application that ships as a container" is regular enough to deserve a shape.
/// </remarks>
public sealed record DotNetServiceSettings : DockerSettings
{
    /// <summary>
    /// The configuration to build and test in.
    /// </summary>
    public string Configuration { get; init; } = "Release";
}
