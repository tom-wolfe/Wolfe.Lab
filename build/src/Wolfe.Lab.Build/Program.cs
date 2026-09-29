using System.CommandLine;
using Ritten.CommandLine;
using Wolfe.Lab.Build;

var app = LabApplication.Create().Build();
if (app.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The lab's jobs. The workflow to run is declared by the ritten.json in the working directory.");
await root.InstallRitten(app.Value);
return await root.Parse(args).InvokeAsync();
