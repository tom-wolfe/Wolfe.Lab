using Ritten.OnePassword;
using Wolfe.Lab.Workflows.Agents;
using Wolfe.Lab.Workflows.Backup;
using Wolfe.Lab.Workflows.CaddyCertificates;
using Wolfe.Lab.Workflows.CaddyRoutes;
using Wolfe.Lab.Workflows.Chezmoi;
using Wolfe.Lab.Workflows.Docker;
using Wolfe.Lab.Workflows.DotNetTool;
using Wolfe.Lab.Workflows.ForgejoRunners;
using Wolfe.Lab.Workflows.GarageLayout;
using Wolfe.Lab.Workflows.GatusHealth;
using Wolfe.Lab.Workflows.Heartbeat;
using Wolfe.Lab.Workflows.ImmichImport;
using Wolfe.Lab.Workflows.Obsidian;
using Wolfe.Lab.Workflows.Ollama;
using Wolfe.Lab.Workflows.Restic;
using Wolfe.Lab.Workflows.Tofu;

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

        return builder;
    }
}
