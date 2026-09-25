using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wolfe.Lab.Build.Workflows.Ollama.Models;

/// <summary>
/// Reads the <c>models</c> section of every ollama component of the slice from the checkout.
/// </summary>
/// <remarks>
/// A component reading its siblings, and deliberately: every server behind <c>ai.twolfe.dev</c>
/// answers for the same role names, so a role that must mean the same model everywhere can
/// only be judged against all of them. Only this slice's own components are read.
/// </remarks>
internal static class SliceComponents
{
    // Ritten's own reading options, so a sibling's file means what it means to its own deploy.
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private const string Workflow = "ollama";

    /// <summary>
    /// Every sibling of the given component — itself included — whose <c>ritten.json</c> runs
    /// this workflow, by directory name.
    /// </summary>
    /// <param name="component">The component being checked.</param>
    public static IReadOnlyDictionary<string, ModelSettings> Read(IDirectory component)
    {
        var slice = Path.GetFullPath(Path.Combine(component.AbsolutePath, ".."));
        var found = new Dictionary<string, ModelSettings>(StringComparer.Ordinal);

        foreach (var directory in Directory.GetDirectories(slice).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var file = Path.Combine(directory, "ritten.json");
            if (!File.Exists(file))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (!document.RootElement.TryGetProperty("workflow", out var workflow) || workflow.GetString() != Workflow)
            {
                continue;
            }

            var settings = document.RootElement.Deserialize<OllamaSettings>(Options);
            found[Path.GetFileName(directory)] = settings?.Models ?? new ModelSettings();
        }

        return found;
    }
}
