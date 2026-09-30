using System.Text;
using Ritten.Git;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Annotates the compose file with the service tags.
/// </summary>
[Step("label services", StepKind.Publish)]
internal sealed class LabelServices(ICommandRunner commands, IGit git, IFileSystem fileSystem, WorkflowJob job, IWorkflowLog log)
{
    internal const string ComposeFile = "compose.yaml";
    internal const string OverrideFile = "compose.override.yaml";

    public async Task<StepResult> Run(Release release, ComposeEnvironment composeEnvironment, CancellationToken ct = default)
    {
        var component = fileSystem.ProjectRoot;
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error($"{component.AbsolutePath} is not in a git checkout, and where a component lives is its path in one.");
        }

        if (Placement.Of(checkout, component) is not { } placement)
        {
            return new Error($"{Path.GetRelativePath(checkout.AbsolutePath, component.AbsolutePath)} is not <area>/<service>/<component>, so its containers cannot say where they live.");
        }

        var services = await Services(component, composeEnvironment, ct);
        var content = Render(placement, services);
        if (job.DryRun)
        {
            log.Skipped($"Would label {Count(services.Count)} of {release.Name} as {placement}.");
            return StepResult.Successful;
        }

        await File.WriteAllTextAsync(Path.Combine(release.Directory.AbsolutePath, OverrideFile), content, ct);
        log.Status($"Labelled {Count(services.Count)} of {release.Name} as {placement}.");
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
    internal static string Render(Placement placement, IReadOnlyList<string> services)
    {
        var yaml = new StringBuilder()
            .AppendLine("# Written by `lab deploy` from where the component sits in the repository:")
            .AppendLine("# the lab's labels on every service (build/README.md). Rewritten on every deploy.")
            .AppendLine("services:");

        foreach (var service in services)
        {
            yaml.AppendLine($"  {service}:")
                .AppendLine("    labels:")
                .AppendLine($"      lab.area: \"{placement.Area}\"")
                .AppendLine($"      lab.service: \"{placement.Service}\"")
                .AppendLine($"      lab.component: \"{placement.Component}\"")
                .AppendLine("    environment:")
                .AppendLine($"      OTEL_RESOURCE_ATTRIBUTES: \"{placement.ResourceAttributes}\"");
        }

        return yaml.ToString();
    }

    private static string Count(int services) => $"{services} service{(services == 1 ? "" : "s")}";
}

/// <summary>
/// Where a component lives: the three directories between the checkout and its declaration.
/// </summary>
internal sealed record Placement(string Area, string Service, string Component)
{
    /// <summary>
    /// The placement as OpenTelemetry's resource attributes spell it.
    /// </summary>
    public string ResourceAttributes => $"lab.area={Area},lab.service={Service},lab.component={Component}";

    public static Placement? Of(IDirectory checkout, IDirectory component) =>
        Path.GetRelativePath(checkout.AbsolutePath, component.AbsolutePath).Split(Path.DirectorySeparatorChar) is [var area, var service, var name]
        && area is not ".." and not "."
            ? new Placement(area, service, name)
            : null;

    public override string ToString() => $"{Area}/{Service}/{Component}";
}
