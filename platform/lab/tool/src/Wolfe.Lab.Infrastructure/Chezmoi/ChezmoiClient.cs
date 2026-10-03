using System.Text.Json;
using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Infrastructure.Chezmoi;

/// <summary>
/// Runs the installed Chezmoi CLI.
/// </summary>
internal sealed class ChezmoiClient(ICommandRunner commands) : IChezmoi
{
    internal const string TokenVariable = "OP_SERVICE_ACCOUNT_TOKEN";
    internal const string StubToken = "render";

    // Prints its last argument: the reference `op read --no-newline <ref>` was given.
    private const string OpStub = "#!/bin/sh\nfor a; do ref=$a; done\nprintf \"%s\" \"$ref\"\n";

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Render(IDirectory source, string profile, IDirectory destination, CancellationToken ct = default)
    {
        // Only the disk makes a directory of a name no other run has.
        var scratch = new PhysicalDirectory(Directory.CreateTempSubdirectory("lab-chezmoi-").FullName);
        try
        {
            var op = scratch.GetFile("op");
            await op.WriteAllText(OpStub, mode: UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, cancellationToken: ct);

            var config = scratch.GetFile("chezmoi.json");
            await config.WriteAllText(JsonSerializer.Serialize(new
            {
                data = new { profile },
                onepassword = new { command = op.AbsolutePath, mode = "service" }
            }), cancellationToken: ct);

            var archive = scratch.GetFile("render.tar");
            await commands.Run(
                Command.Create("chezmoi")
                    .WithArguments(
                        "--source", source.AbsolutePath,
                        "--destination", destination.AbsolutePath,
                        "--config", config.AbsolutePath,
                        "--cache", scratch.GetDirectory("cache").AbsolutePath,
                        "archive", "--exclude", "externals", "--output", archive.AbsolutePath)
                    .WithEnvironmentVariables(new Dictionary<string, string> { [TokenVariable] = StubToken })
                    .ThrowOnError(),
                ct);

            // The system tar, not .NET's reader: chezmoi writes headers the latter cannot parse.
            destination.Create();
            await commands.Run(Command.Create("tar").WithArguments("-x", "-C", destination.AbsolutePath, "-f", archive.AbsolutePath).ThrowOnError(), ct);
        }
        finally
        {
            // rm, not Directory.Delete: chezmoi's HTTP cache files a download under a path
            // longer than .NET will traverse, and the scratch must go whatever it holds.
            await commands.Run(Command.Create("rm").WithArguments("-rf", scratch.AbsolutePath), CancellationToken.None);
        }

        // Everything in a home tree is a dotfile, which the glob matches as any other name.
        return [.. destination.GetFiles("**/*").Select(destination.RelativePath).Order(StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public async Task Update(CancellationToken ct = default) =>
        await commands.Run(Command.Create("chezmoi").WithArguments("update", "--init", "--no-tty").ThrowOnError(), ct);
}
