using System.Globalization;
using System.Text.Json;
using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Compose;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Writes a file to disk announcing where this service exposes its metrics.
/// </summary>
/// <remarks>
/// A target file per component in <c>${LAB_ROOT}/.metrics</c>, in the file discovery format the
/// collector watches (monitoring/alloy): each endpoint at the node's loopback port the deploy
/// published it on, named for its container and labelled with the component's place. Rewritten
/// on every deploy, so an endpoint the component no longer declares stops being scraped; a
/// component that declares none has no file.
/// </remarks>
[Step("declare metrics", StepKind.Publish)]
internal sealed class DeclareMetrics(IOptions<LabDirectories> options, WorkflowJob job, IWorkflowLog log)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public async Task<StepResult> Run(ComposeBindings bindings, CancellationToken ct = default)
    {
        var directory = options.Value.Metrics;
        foreach (var binding in bindings.Bindings)
        {
            var file = directory.GetFile(binding.Component.QualifiedName + ".json");
            var targets = Targets(binding);
            if (job.DryRun)
            {
                log.Skipped(targets.Count == 0
                    ? $"Would declare no metrics for {binding.Component}."
                    : $"Would declare {Endpoints(targets.Count)} for {binding.Component}: {string.Join(", ", targets.Select(target => target.Targets[0] + target.Labels[MetricsTargetFile.PathLabel]))}.");
                continue;
            }

            if (targets.Count == 0)
            {
                file.Delete();
                continue;
            }

            await file.WriteAllText(JsonSerializer.Serialize(targets, Indented), cancellationToken: ct);
            log.Status($"Declared {Endpoints(targets.Count)} for {binding.Component}.");
        }

        return StepResult.Successful;
    }

    /// <summary>
    /// One target per endpoint: its address on the node's loopback, its path, its container's name
    /// and where the component lives, each attribute spelled as the store's labels are
    /// (<see cref="TelemetryAttribute.Label"/>).
    /// </summary>
    private static IReadOnlyList<DiscoveryTarget> Targets(ComposeBinding binding) =>
    [
        .. binding.Metrics.Select(target => new DiscoveryTarget(
            [$"127.0.0.1:{target.Port.Value.ToString(CultureInfo.InvariantCulture)}"],
            new Dictionary<string, string>([
                new KeyValuePair<string, string>(MetricsTargetFile.PathLabel, target.Path.Value),
                new KeyValuePair<string, string>(TelemetryAttribute.ServiceName.Label, binding.Service.Container),
                .. binding.Component.Attributes.Select(attribute => KeyValuePair.Create(attribute.Key.Label, attribute.Value))
            ])))
    ];

    private static string Endpoints(int count) => $"{count} metrics endpoint{(count == 1 ? "" : "s")}";
}
