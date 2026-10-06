using System.Text.RegularExpressions;
using Polly.Registry;
using Ritten.Docker;
using Wolfe.Lab.Infrastructure.Resilience;

namespace Wolfe.Lab.Infrastructure.Garage;

/// <summary>
/// <c>docker exec garage /garage …</c>.
/// </summary>
internal sealed partial class GarageClient(IDocker docker, ResiliencePipelineProvider<string> pipelines) : IGarage
{
    internal const string Container = "garage";

    /// <summary>
    /// The wait for the daemon to answer, configured under <c>Garage:Answering</c>.
    /// </summary>
    internal const string Answering = "garage.answering";

    /// <inheritdoc />
    public async Task<bool> AwaitReady(CancellationToken ct = default) =>
        await pipelines.GetPipeline<bool>(Answering).Until(async inner => await Answers(inner), ct: ct);

    /// <inheritdoc />
    public async Task<int> LayoutVersion(CancellationToken ct = default)
    {
        var result = await docker.Exec(Read("layout", "show"), ct);
        var match = VersionLine().Match(result.StandardOutput);
        return match.Success
            ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)
            : throw new CommandFailedException("garage layout show did not say which layout version is current.", result);
    }

    /// <inheritdoc />
    public async Task<string> NodeId(CancellationToken ct = default)
    {
        var result = await docker.Exec(Read("node", "id", "-q"), ct);
        // The layout commands take the id's prefix; sixteen characters is what the docs use.
        var id = result.StandardOutput.Trim();
        return id.Length > 16 ? id[..16] : id;
    }

    /// <inheritdoc />
    public async Task AssignLayout(string nodeId, string zone, string capacity, CancellationToken ct = default) =>
        await docker.Exec(Garage("layout", "assign", "-z", zone, "-c", capacity, nodeId), ct);

    /// <inheritdoc />
    public async Task ApplyLayout(int version, CancellationToken ct = default) =>
        await docker.Exec(Garage("layout", "apply", "--version", version.ToString(System.Globalization.CultureInfo.InvariantCulture)), ct);

    // A daemon still starting fails status: the answer, not an error.
    private async Task<bool> Answers(CancellationToken ct)
    {
        try
        {
            await docker.Exec(Read("status"), ct);
            return true;
        }
        catch (CommandFailedException)
        {
            return false;
        }
    }

    private static ContainerExec Garage(params string[] arguments) => new(Container, ["/garage", .. arguments]);

    private static ContainerExec Read(params string[] arguments) => Garage(arguments) with { IsReadOnly = true };

    [GeneratedRegex(@"^Current cluster layout version: (\d+)", RegexOptions.Multiline)]
    private static partial Regex VersionLine();
}
