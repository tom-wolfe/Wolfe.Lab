using Ritten.OnePassword;
using Wolfe.Lab.Build.Workflows.Agents;
using Wolfe.Lab.Build.Workflows.Backup;
using Wolfe.Lab.Build.Workflows.CaddyCertificates;
using Wolfe.Lab.Build.Workflows.CaddyRoutes;
using Wolfe.Lab.Build.Workflows.Chezmoi;
using Wolfe.Lab.Build.Workflows.Docker;
using Wolfe.Lab.Build.Workflows.DotNetTool;
using Wolfe.Lab.Build.Workflows.ForgejoRunners;
using Wolfe.Lab.Build.Workflows.GarageLayout;
using Wolfe.Lab.Build.Workflows.GatusHealth;
using Wolfe.Lab.Build.Workflows.Heartbeat;
using Wolfe.Lab.Build.Workflows.ImmichImport;
using Wolfe.Lab.Build.Workflows.Obsidian;
using Wolfe.Lab.Build.Workflows.Ollama;
using Wolfe.Lab.Build.Workflows.Restic;
using Wolfe.Lab.Build.Workflows.Tofu;

namespace Wolfe.Lab.Build;

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
        // The regular shapes — a compose stack, a tofu root, a backup — carry most of the lab; the rest are the components only one slice has.
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
