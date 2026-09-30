using Wolfe.Lab.Clients.Releases;
using Wolfe.Lab.Values;
using Wolfe.Lab.Workflows.Docker.Models;
using YamlDotNet.Serialization;

namespace Wolfe.Lab.Workflows.Docker.Steps;

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
    /// Renders the compose.override.yaml
    /// </summary>
    internal static string Render(Component component, IReadOnlyList<string> services)
    {
        var labels = component.Attributes.ToDictionary(attribute => attribute.Key.Name, attribute => attribute.Value);
        var environment = new Dictionary<string, string> { [TelemetryAttribute.ResourceAttributesVariable] = component.ResourceAttributes };
        var document = new Dictionary<string, object>
        {
            ["services"] = services.ToDictionary(service => service, _ => new Dictionary<string, object>
            {
                ["labels"] = labels,
                ["environment"] = environment
            })
        };

        return Header + Yaml.Serialize(document);
    }

    // A comment is the one thing a serializer does not write.
    private const string Header = """
        # Written by `lab deploy` from where the component sits in the repository: the lab's
        # labels on every service (platform/lab/README.md). Rewritten on every deploy.

        """;

    private static readonly ISerializer Yaml = new SerializerBuilder().WithQuotingNecessaryStrings().DisableAliases().Build();

    private static string Count(int services) => $"{services} service{(services == 1 ? "" : "s")}";
}
