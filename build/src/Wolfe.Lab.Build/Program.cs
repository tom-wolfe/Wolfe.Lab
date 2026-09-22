using System.CommandLine;
using Ritten.CommandLine;
using Ritten.OnePassword;
using Wolfe.Lab.Build;
using Wolfe.Lab.Build.Workflows.Caddy;
using Wolfe.Lab.Build.Workflows.Chezmoi;
using Wolfe.Lab.Build.Workflows.Docker;
using Wolfe.Lab.Build.Workflows.Files;
using Wolfe.Lab.Build.Workflows.Forgejo;
using Wolfe.Lab.Build.Workflows.Garage;
using Wolfe.Lab.Build.Workflows.Gatus;
using Wolfe.Lab.Build.Workflows.Heartbeat;
using Wolfe.Lab.Build.Workflows.Immich;
using Wolfe.Lab.Build.Workflows.Obsidian;
using Wolfe.Lab.Build.Workflows.Ollama;
using Wolfe.Lab.Build.Workflows.Restic;
using Wolfe.Lab.Build.Workflows.Service;
using Wolfe.Lab.Build.Workflows.Tofu;

var builder = WorkflowApplication.CreateBuilder();

builder.Workflows
    .Add<ObsidianWorkflow>()
    .Add<ImmichWorkflow>()
    .Add<FilesWorkflow>()
    .Add<ResticWorkflow>()
    .Add<ServiceWorkflow>()
    .Add<OllamaWorkflow>()
    .Add<CaddyWorkflow>()
    .Add<DockerWorkflow>()
    .Add<DotNetServiceWorkflow>()
    .Add<ImageWorkflow>()
    .Add<TofuWorkflow>()
    .Add<ChezmoiWorkflow>()
    .Add<HeartbeatWorkflow>()
    .Add<GatusWorkflow>()
    .Add<ForgejoWorkflow>()
    .Add<GarageWorkflow>();

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
