using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using Ritten.Git;
using Ritten.OnePassword;
using Ritten.OpenTelemetry;
using Wolfe.Lab.Application.Telemetry;
using Wolfe.Lab.Application.Workflows.Agents;
using Wolfe.Lab.Application.Workflows.Backup;
using Wolfe.Lab.Application.Workflows.CaddyCertificates;
using Wolfe.Lab.Application.Workflows.CaddyRoutes;
using Wolfe.Lab.Application.Workflows.Chezmoi;
using Wolfe.Lab.Application.Workflows.Docker;
using Wolfe.Lab.Application.Workflows.DotNetTool;
using Wolfe.Lab.Application.Workflows.ForgejoRunners;
using Wolfe.Lab.Application.Workflows.GarageLayout;
using Wolfe.Lab.Application.Workflows.GatusHealth;
using Wolfe.Lab.Application.Workflows.Heartbeat;
using Wolfe.Lab.Application.Workflows.ImmichImport;
using Wolfe.Lab.Application.Workflows.Obsidian;
using Wolfe.Lab.Application.Workflows.Ollama;
using Wolfe.Lab.Application.Workflows.Restic;
using Wolfe.Lab.Application.Workflows.Tofu;
using Wolfe.Lab.Infrastructure;

namespace Wolfe.Lab;

/// <summary>
/// The lab's application: every workflow, its runtime and the vault.
/// </summary>
public static class LabApplication
{
    /// <summary>
    /// A builder with everything the lab registers.
    /// </summary>
    public static WorkflowApplicationBuilder Create()
    {
        var builder = WorkflowApplication.CreateBuilder();

        // One workflow per component shape.
        // The regular shapes — a compose stack, a tofu root, a backup — carry most of the lab; the rest are the components only one service has.
        builder.Workflows
            .Add<DockerWorkflow>()
            .Add<DotNetServiceWorkflow>()
            .Add<ImageWorkflow>()
            .Add<DotNetToolWorkflow>()
            .Add<TofuWorkflow>()
            .Add<BackupWorkflow>()
            .Add<ChezmoiWorkflow>()
            .Add<ObsidianWorkflow>()
            .Add<OllamaWorkflow>()
            .Add<AgentsWorkflow>()
            .Add<ResticWorkflow>()
            .Add<HeartbeatWorkflow>()
            .Add<CaddyCertificatesWorkflow>()
            .Add<CaddyRoutesWorkflow>()
            .Add<ForgejoRunnersWorkflow>()
            .Add<GarageLayoutWorkflow>()
            .Add<GatusHealthWorkflow>()
            .Add<ImmichImportWorkflow>();

        builder.Runtimes
            .Add<LabRuntime>();

        builder
            .AddOnePassword(options => options.ServiceAccountTokenFile = LabConfiguration.Current["OnePassword:ServiceAccountTokenFile"]
            is { Length: > 0 } file ? file : throw new InvalidOperationException("'OnePassword:ServiceAccountTokenFile' is not set in appsettings.json."));

        // Traced where an OTLP endpoint is set: each run as the lab's, labelled with the component it runs for.
        builder.AddOpenTelemetry(telemetry => telemetry.ConfigureResource(resource => resource
            .AddService("lab", serviceVersion: typeof(LabApplication).Assembly.GetName().Version?.ToString())
            .AddDetector(services => new ComponentResourceDetector(services.GetRequiredService<IGit>(), services.GetRequiredService<IFileSystem>()))));

        return builder;
    }
}
