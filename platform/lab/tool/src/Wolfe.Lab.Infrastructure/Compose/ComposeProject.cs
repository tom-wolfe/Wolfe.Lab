using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// A component's compose stack as compose itself resolves it.
/// </summary>
/// <param name="Services">Every service, by name.</param>
public sealed record ComposeProject(IReadOnlyList<ComposeService> Services)
{
    /// <summary>
    /// The stack in <paramref name="directory"/>, through <c>docker compose config</c>.
    /// </summary>
    /// <remarks>
    /// Its files are compose's to find (<see cref="DefaultFiles"/>), as <c>compose up</c> finds
    /// them, so what is read is what runs. Only ever a checkout's directory: there, an override
    /// is one committed, and part of the stack; the one the deploy writes is in the release.
    /// </remarks>
    /// <param name="commands">What runs compose.</param>
    /// <param name="directory">The component's directory, which holds its compose files.</param>
    /// <param name="environment">What the file interpolates: a deploy's secrets, resolved; none, to a check, which leaves them unset.</param>
    /// <param name="ct">A token to monitor for cancellation.</param>
    public static async Task<Result<ComposeProject>> Read(ICommandRunner commands, IDirectory directory, IReadOnlyDictionary<string, string>? environment = null,
        CancellationToken ct = default)
    {
        var result = await commands.Run(Command.Create("docker")
            .WithArguments("compose", "--project-directory", directory.AbsolutePath, "config", "--format", "json")
            .WithEnvironmentVariables(environment ?? new Dictionary<string, string>())
            .QuietOutput(), ct);
        return result.ExitCode.Value == 0
            ? Parse(result.StandardOutput)
            : ComposeErrors.Unreadable(directory, result.StandardError.Trim());
    }

    /// <summary>
    /// The files compose looks for when none is named, in the order it looks: the compose
    /// specification's, not the lab's. The first it finds is the stack's, an override beside it
    /// merged in.
    /// </summary>
    public static IReadOnlyList<string> DefaultFiles { get; } = ["compose.yaml", "compose.yml", "docker-compose.yaml", "docker-compose.yml"];

    /// <summary>
    /// The project as <c>docker compose config --format json</c> prints it.
    /// </summary>
    public static Result<ComposeProject> Parse(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize<Document>(json);
            return new ComposeProject([
                .. (document?.Services ?? []).Select(service => new ComposeService(
                        service.Key,
                        service.Value.Labels ?? [],
                        service.Value.Environment ?? [],
                        [.. (service.Value.Ports ?? []).Select(port => port.ToPort())],
                        service.Value.NetworkMode)
                    {
                        ContainerName = service.Value.ContainerName
                    })
                    .OrderBy(service => service.Name, StringComparer.Ordinal)
            ]);
        }
        catch (JsonException e)
        {
            return ComposeErrors.NotConfiguration(e.Message);
        }
    }

    private sealed record Document([property: JsonPropertyName("services")] Dictionary<string, Definition>? Services);

    private sealed record Definition(
        [property: JsonPropertyName("labels")] Dictionary<string, string>? Labels,
        [property: JsonPropertyName("environment")] Dictionary<string, string?>? Environment,
        [property: JsonPropertyName("ports")] List<Port>? Ports,
        [property: JsonPropertyName("network_mode")] string? NetworkMode,
        [property: JsonPropertyName("container_name")] string? ContainerName = null);

    // Compose prints a published port as a string, which is a range when the file gave one.
    private sealed record Port(
        [property: JsonPropertyName("target")] int Target,
        [property: JsonPropertyName("published")] string? Published,
        [property: JsonPropertyName("host_ip")] string? HostIp,
        [property: JsonPropertyName("protocol")] string? Protocol)
    {
        public ComposePort ToPort() =>
            new(Target, int.TryParse(Published, out var published) ? published : null, HostIp, Protocol ?? "tcp");
    }
}
