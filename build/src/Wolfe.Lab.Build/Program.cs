using System.CommandLine;
using Ritten.CommandLine;
using Ritten.OnePassword;
using Wolfe.Lab.Build;
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

var builder = WorkflowApplication.CreateBuilder();

// One workflow per component shape. The regular shapes — a compose stack, a tofu root, a
// backup — carry most of the lab; the rest are the components only one slice has.
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

builder.AddOnePassword(options => options.ServiceAccountTokenFile = "~/Docker/1password/service-account-token");

var built = builder.Build();
if (built.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The lab's jobs. The workflow to run is declared by the ritten.json in the working directory.");
await root.InstallRitten(built.Value);
return await root.Parse(args).InvokeAsync();
