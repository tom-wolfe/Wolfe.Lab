namespace Wolfe.Lab.Clients.Telemetry.Steps;

/// <summary>
/// Holds the component's configuration to the lab's one list of telemetry attributes.
/// </summary>
[Step("check telemetry names", StepKind.Check)]
internal sealed class CheckTelemetryNames(IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string ComposeFile = "compose.yaml";

    // Scratch and output, the CLI's and the tools', which are nobody's configuration.
    private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal) { "temp", "artifacts", "bin", "obj", "node_modules" };

    public StepResult Run()
    {
        var component = fileSystem.ProjectRoot;
        var problems = Check(component.AbsolutePath);
        if (problems.Count > 0)
        {
            return StepResult.Failed([.. problems.Select(problem => new Error(problem))]);
        }

        log.Detail("Every telemetry name is one the lab knows.");
        return StepResult.Successful;
    }

    /// <summary>
    /// What is wrong with the component's configuration, each problem prefixed with its file.
    /// </summary>
    internal static IReadOnlyList<string> Check(string component)
    {
        var problems = new List<string>();
        foreach (var file in Files(component))
        {
            var text = File.ReadAllText(file);
            var name = Path.GetFileName(file);
            var found = Path.GetExtension(file) switch
            {
                ".alloy" => TelemetryNames.Alloy(text),
                ".yaml" or ".yml" when name == ComposeFile => TelemetryNames.Compose(text).Concat(TelemetryNames.Yaml(text)),
                ".yaml" or ".yml" => TelemetryNames.Yaml(text),
                _ => []
            };

            problems.AddRange(found.Select(problem => $"{Path.GetRelativePath(component, file)}: {problem}"));
        }

        return problems;
    }

    private static IEnumerable<string> Files(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory).Order(StringComparer.Ordinal))
        {
            yield return file;
        }

        foreach (var child in Directory.EnumerateDirectories(directory).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(child);
            if (name.StartsWith('.') || Skipped.Contains(name))
            {
                continue;
            }

            foreach (var file in Files(child))
            {
                yield return file;
            }
        }
    }
}
