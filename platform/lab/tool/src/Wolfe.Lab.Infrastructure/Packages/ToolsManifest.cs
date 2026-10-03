using System.Text.Json;

namespace Wolfe.Lab.Infrastructure.Packages;

/// <summary>
/// <c>.config/lab-tools.json</c>: the programs the lab's jobs run, pinned, one entry each.
/// </summary>
/// <remarks>
/// Beside <c>dotnet-tools.json</c>, and the same idea: the repository names the versions its jobs
/// run, rather than each node deciding by what it happened to install.
/// </remarks>
public sealed record ToolsManifest
{
    /// <summary>
    /// The manifest's file name, under the checkout's <c>.config/</c>.
    /// </summary>
    public const string FileName = "lab-tools.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The tools, by the command a job runs.
    /// </summary>
    public IReadOnlyDictionary<string, PackageOptions> Tools { get; init; } = new Dictionary<string, PackageOptions>();

    /// <summary>
    /// The manifest in the checkout, or an empty one when it has none.
    /// </summary>
    public static async Task<ToolsManifest> Read(IDirectory checkout, CancellationToken ct)
    {
        var file = checkout.GetDirectory(".config").GetFile(FileName);
        if (!file.Exists)
        {
            return new ToolsManifest();
        }

        await using var stream = file.OpenRead();
        return await JsonSerializer.DeserializeAsync<ToolsManifest>(stream, Options, ct) ?? new ToolsManifest();
    }
}
