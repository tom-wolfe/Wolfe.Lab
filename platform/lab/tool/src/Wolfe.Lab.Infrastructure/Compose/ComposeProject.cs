using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// A component's compose file as compose itself resolves it: labels and environment read as maps
/// whichever way the file spells them.
/// </summary>
/// <param name="Services">Every service, by name.</param>
public sealed record ComposeProject(IReadOnlyList<ComposeService> Services)
{
    /// <summary>
    /// The compose file in <paramref name="directory"/>, through <c>docker compose config</c>.
    /// </summary>
    /// <remarks>
    /// Named with <c>-f</c>, never left to compose to find: an override beside it — the deploy's,
    /// in a release — is the deploy's to merge, not the component's to declare.
    /// </remarks>
    /// <param name="commands">What runs compose.</param>
    /// <param name="directory">The component's directory, which holds <c>compose.yaml</c>.</param>
    /// <param name="ct">A token to monitor for cancellation.</param>
    public static async Task<Result<ComposeProject>> Read(ICommandRunner commands, string directory, CancellationToken ct = default)
    {
        var result = await commands.Run(Command.Create("docker")
            .WithArguments("compose", "--project-directory", directory, "-f", Path.Combine(directory, FileName), "config", "--format", "json")
            .QuietOutput(), ct);
        return result.ExitCode.Value == 0
            ? Parse(result.StandardOutput)
            : new Error($"compose cannot read {FileName}: {result.StandardError.Trim()}");
    }

    /// <summary>
    /// The file a compose component's stack is declared in.
    /// </summary>
    public const string FileName = "compose.yaml";

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
                        service.Value.Environment ?? []))
                    .OrderBy(service => service.Name, StringComparer.Ordinal)
            ]);
        }
        catch (JsonException e)
        {
            return new Error($"compose printed something that is not its configuration: {e.Message}");
        }
    }

    private sealed record Document([property: JsonPropertyName("services")] Dictionary<string, Definition>? Services);

    private sealed record Definition(
        [property: JsonPropertyName("labels")] Dictionary<string, string>? Labels,
        [property: JsonPropertyName("environment")] Dictionary<string, string?>? Environment);
}

/// <summary>
/// One service of a <see cref="ComposeProject"/>.
/// </summary>
/// <param name="Name">The service's name in the compose file.</param>
/// <param name="Labels">Its Docker labels.</param>
/// <param name="Environment">Its environment; a variable declared without a value is null.</param>
public sealed record ComposeService(string Name, IReadOnlyDictionary<string, string> Labels, IReadOnlyDictionary<string, string?> Environment);
