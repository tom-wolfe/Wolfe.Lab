using System.CommandLine;
using Ritten.CommandLine;
using Wolfe.Lab.Build.Agents.Workflows;
using Wolfe.Lab.Build.Caddy.Workflows;
using Wolfe.Lab.Build.Docker.Workflows;
using Wolfe.Lab.Build.DotNet.Workflows;
using Wolfe.Lab.Build.Files.Workflows;
using Wolfe.Lab.Build.Immich.Workflows;
using Wolfe.Lab.Build.Obsidian.Workflows;
using Wolfe.Lab.Build.Ollama.Workflows;
using Wolfe.Lab.Build.Restic.Workflows;
using Wolfe.Lab.Build.Runtimes;
using Wolfe.Lab.Build.Service.Workflows;

var builder = WorkflowApplication.CreateBuilder();

builder.Workflows
    .Add<ObsidianWorkflow>()
    .Add<ImmichWorkflow>()
    .Add<FilesWorkflow>()
    .Add<ResticWorkflow>()
    .Add<ServiceWorkflow>()
    .Add<AgentsWorkflow>()
    .Add<OllamaWorkflow>()
    .Add<CaddyWorkflow>()
    .Add<DockerWorkflow>()
    .Add<DotNetServiceWorkflow>()
    .Add<ImageWorkflow>();

builder.Runtimes
    .Add<LabRuntime>();

var built = builder.Build();
if (built.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The lab's jobs. The workflow to run is declared by the ritten.json in the working directory.");
await root.InstallRitten(built.Value);

return await root.Parse(args).InvokeAsync();
