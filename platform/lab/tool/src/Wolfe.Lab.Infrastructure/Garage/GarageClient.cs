using System.Globalization;
using System.Text.Json;
using Polly.Registry;
using Ritten.Docker;
using Wolfe.Lab.Domain.Catalog.Components.Garage;
using Wolfe.Lab.Infrastructure.Resilience;

namespace Wolfe.Lab.Infrastructure.Garage;

/// <summary>
/// <c>docker exec garage /garage …</c>.
/// </summary>
internal sealed class GarageClient(IDocker docker, ResiliencePipelineProvider<string> pipelines) : IGarage
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
    public async Task<ClusterLayout> Layout(CancellationToken ct = default)
    {
        // The admin API's answer, not layout show's: that rounds each capacity to a tenth of its unit.
        var result = await docker.Exec(Read("json-api", "GetClusterLayout"), ct);
        var layout = JsonSerializer.Deserialize<LayoutReply>(result.StandardOutput, Replies)
                     ?? throw new CommandFailedException("garage json-api GetClusterLayout answered nothing.", result);
        var roles = layout.Roles
            .Where(role => role.Capacity is not null)
            .ToDictionary(role => role.Id, role => new GarageLayout(GarageZone.From(role.Zone), StorageCapacity.From(role.Capacity!.Value)), StringComparer.Ordinal);
        return new ClusterLayout(layout.Version, roles);
    }

    /// <inheritdoc />
    public async Task<string> NodeId(CancellationToken ct = default)
    {
        // <id>@<address>: the layout names a node by the id alone.
        var result = await docker.Exec(Read("node", "id", "-q"), ct);
        return result.StandardOutput.Trim().Split('@')[0];
    }

    /// <inheritdoc />
    public async Task AssignLayout(string nodeId, GarageLayout layout, CancellationToken ct = default) =>
        await docker.Exec(Garage("layout", "assign", "-z", layout.Zone.Value, "-c", layout.Capacity.Value.ToString(CultureInfo.InvariantCulture), nodeId), ct);

    /// <inheritdoc />
    public async Task ApplyLayout(int version, CancellationToken ct = default) =>
        await docker.Exec(Garage("layout", "apply", "--version", version.ToString(CultureInfo.InvariantCulture)), ct);

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

    private static readonly JsonSerializerOptions Replies = new() { PropertyNameCaseInsensitive = true };

    private sealed record LayoutReply(int Version, IReadOnlyList<RoleReply> Roles);

    private sealed record RoleReply(string Id, string Zone, long? Capacity);
}
