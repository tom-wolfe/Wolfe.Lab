using System.Text;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Annotates the compose file with the service tags.
/// </summary>
[Step("label services", StepKind.Publish)]
internal sealed class LabelServices(ICommandRunner commands, IFileSystem fileSystem, WorkflowJob job, IWorkflowLog log)
{
    internal const string ComposeFile = "compose.yaml";
    internal const string OverrideFile = "compose.override.yaml";

    public async Task<StepResult> Run(Release release, Component component, ComposeEnvironment composeEnvironment, CancellationToken ct = default)
    {
        var services = await Services(fileSystem.ProjectRoot, composeEnvironment, ct);
        var content = Render(component, services);
        if (job.DryRun)
        {
            log.Skipped($"Would label {Count(services.Count)} of {release.Name} as {component}.");
            return StepResult.Successful;
        }

        await File.WriteAllTextAsync(Path.Combine(release.Directory.AbsolutePath, OverrideFile), content, ct);
        log.Status($"Labelled {Count(services.Count)} of {release.Name} as {component}.");
        return StepResult.Successful;
    }

    /// <summary>
    /// The services compose reads out of the component's own file — the checkout's copy, which
    /// the release was just published from, so a rehearsal of a first deploy can read it too.
    /// </summary>
    /// <remarks>
    /// Named with <c>-f</c>, never left to compose to find: an override from an earlier deploy
    /// naming a service the file has since dropped would read as a service with no image.
    /// </remarks>
    private async Task<IReadOnlyList<string>> Services(IDirectory component, ComposeEnvironment composeEnvironment, CancellationToken ct)
    {
        var command = Command.Create("docker")
            .WithArguments("compose", "--project-directory", component.AbsolutePath,
                "-f", component.GetFile(ComposeFile).AbsolutePath, "config", "--services")
            .WithEnvironmentVariables(composeEnvironment.Variables)
            .QuietOutput()
            .ThrowOnError();

        var result = await commands.Run(command, ct);
        return [.. result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// The override itself. Its values are directory names and service names, neither of which
    /// needs escaping in a double-quoted YAML string.
    /// </summary>
    internal static string Render(Component component, IReadOnlyList<string> services)
    {
        var yaml = new StringBuilder()
            .AppendLine("# Written by `lab deploy` from where the component sits in the repository:")
            .AppendLine("# the lab's labels on every service (build/README.md). Rewritten on every deploy.")
            .AppendLine("services:");

        foreach (var service in services)
        {
            yaml.AppendLine($"  {service}:")
                .AppendLine("    labels:");
            foreach (var (attribute, value) in component.Attributes)
            {
                yaml.AppendLine($"      {attribute.Name}: \"{value}\"");
            }

            yaml.AppendLine("    environment:")
                .AppendLine($"      OTEL_RESOURCE_ATTRIBUTES: \"{component.ResourceAttributes}\"");
        }

        return yaml.ToString();
    }

    private static string Count(int services) => $"{services} service{(services == 1 ? "" : "s")}";
}
