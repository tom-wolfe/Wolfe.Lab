using System.CommandLine;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab;

/// <summary>
/// Emits the JSON schema used to validate the lab's service catalog metadata.
/// </summary>
/// <remarks>
/// Run automatically during the build.
/// </remarks>
internal static class SchemaCommand
{
    /// <summary>
    /// Usage: <c>lab schema &lt;file&gt;</c>.
    /// </summary>
    public static System.CommandLine.Command Create()
    {
        var file = new Argument<FileInfo>("file") { Description = "Where to write the schema." };
        var command = new System.CommandLine.Command("schema", "Writes the declaration schema.");
        command.Arguments.Add(file);
        command.SetAction(async (parse, ct) =>
        {
            if (parse.GetValue(file) is not { } target)
            {
                return ExitCode.ConfigurationError;
            }

            await new PhysicalFile(target.FullName).WriteAllText(LabSchema.Json, cancellationToken: ct);
            return 0;
        });
        return command;
    }
}
