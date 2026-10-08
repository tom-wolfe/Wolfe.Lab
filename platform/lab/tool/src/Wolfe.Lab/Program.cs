using System.CommandLine;
using Ritten.CommandLine;
using Wolfe.Lab;
var app = LabApplication.Create().Build();
if (app.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The lab's jobs. The workflow to run is the one the working directory's components declare, or its ritten.json names.");
root.Subcommands.Add(SchemaCommand.Create());
await root.InstallRitten(app.Value);
return await root.Parse(args).InvokeAsync();
