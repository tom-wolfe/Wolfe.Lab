using System.Diagnostics;

namespace Wolfe.Lab.Build.Tests;

/// <summary>
/// Runs a command for real, for tests that exercise the tool itself — rsync — rather than a
/// substitute for it. Ritten's own runner is internal to it.
/// </summary>
internal sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default)
    {
        var info = new ProcessStartInfo(command.Path) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (command.WorkingDirectory is { } directory)
        {
            info.WorkingDirectory = directory;
        }

        foreach (var argument in command.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (command.ThrowsOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{command.Path} exited {process.ExitCode}: {await error}");
        }

        return new CommandResult(new ExitCode(process.ExitCode), await output, await error);
    }
}
