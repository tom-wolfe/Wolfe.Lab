using Ritten.Docker;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Telemetry;
using YamlDotNet.Serialization;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Annotates the compose file with the service tags.
/// </summary>
[Step("label services", StepKind.Publish)]
internal sealed class LabelServices(IDocker docker, IFileSystem fileSystem, IOptions<LabDirectories> options, WorkflowJob job, IWorkflowLog log)
{
    internal const string OverrideFile = "compose.override.yaml";

    public async Task<StepResult<ComposeBindings>> Run(DeploymentUnit unit, ComposeEnvironment composeEnvironment, CancellationToken ct = default)
    {
        if (!(await docker.ComposeConfig(fileSystem.ProjectRoot, composeEnvironment.Variables, ct)).TryGetValue(out var project, out var unreadable))
        {
            return StepResult.Failed(ComposeErrors.Unreadable(fileSystem.ProjectRoot, unreadable));
        }

        if (!ComposeBindings.Of(unit, project).TryGetValue(out var bindings, out var unfit))
        {
            return StepResult.Failed(unfit);
        }

        var content = Render(bindings);
        if (job.DryRun)
        {
            log.Skipped($"Would label {Labelled(bindings)} of {unit.Name}{Asked(bindings)}.");
            return bindings;
        }

        await options.Value.DeployedTo(unit).GetFile(OverrideFile).WriteAllText(content, cancellationToken: ct);
        log.Status($"Labelled {Labelled(bindings)} of {unit.Name}{Asked(bindings)}.");
        return bindings;
    }

    /// <summary>
    /// Renders the compose.override.yaml: for each service.
    /// </summary>
    internal static string Render(ComposeBindings bindings)
    {
        var document = new Dictionary<string, object>
        {
            ["services"] = bindings.Bindings.ToDictionary(binding => binding.Service.Name, object (binding) =>
            {
                var labels = binding.Component.Attributes.ToDictionary(attribute => attribute.Key.Name, attribute => attribute.Value);
                foreach (var (label, value) in binding.Labels)
                {
                    labels[label.Name] = value;
                }

                var definition = new Dictionary<string, object>
                {
                    ["labels"] = labels,
                    ["environment"] = new Dictionary<string, string> { [TelemetryAttribute.ResourceAttributesVariable] = binding.Component.ResourceAttributes }
                };
                if (binding.Publishes.Count > 0)
                {
                    definition["ports"] = binding.Publishes;
                }

                return definition;
            })
        };

        return Header + Yaml.Serialize(document);
    }

    // A comment is the one thing a serializer does not write.
    private const string Header = """
        # Written by `lab deploy` from the components declared beside this stack: on each service,
        # where its component lives, and what the component declares — the collector's labels,
        # and the ports it scrapes on (platform/lab/README.md). Rewritten on every deploy.

        """;

    private static readonly ISerializer Yaml = new SerializerBuilder().WithQuotingNecessaryStrings().DisableAliases().Build();

    /// <summary>
    /// What the components ask of their services, for the log: <c>; logs over OTLP from watcher;
    /// metrics from bridge on loopback's 9090, at /metrics</c>.
    /// </summary>
    private static string Asked(ComposeBindings bindings)
    {
        var logs = bindings.Bindings.Where(binding => binding.Labels.ContainsKey(ContainerLabel.Logs)).Select(binding => binding.Component.Name.Value).ToList();
        var metrics = bindings.Bindings
            .SelectMany(binding => binding.Metrics.Select(target => $"{binding.Component.Name} on loopback's {target.Port}, at {target.Path}"))
            .ToList();
        return (logs.Count > 0 ? $"; logs over OTLP from {string.Join(", ", logs)}" : "")
               + (metrics.Count > 0 ? $"; metrics from {string.Join(", ", metrics)}" : "");
    }

    private static string Labelled(ComposeBindings bindings) =>
        $"{bindings.Bindings.Count} service{(bindings.Bindings.Count == 1 ? "" : "s")} as "
        + string.Join(", ", bindings.Bindings.Select(binding => binding.Component.ToString()));
}
