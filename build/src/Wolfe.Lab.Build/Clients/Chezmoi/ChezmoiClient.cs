namespace Wolfe.Lab.Build.Clients.Chezmoi;

/// <summary>
/// Runs the <c>chezmoi</c> on the path.
/// </summary>
/// <remarks>
/// A render uses a scratch config that supplies the profile and points <c>onepassword.command</c>
/// at a stub, so every <c>onepasswordRead</c> renders as its own <c>op://</c> reference and the
/// vault is never touched. Externals are skipped — downloads, not templates. Scripts render like
/// files, under <c>.chezmoiscripts/</c>. A file gated to other profiles is simply absent.
/// <c>.chezmoi.homeDir</c> is this machine's, so home-rooted paths read as the renderer's home:
/// the shape is what to review, not the prefix.
/// </remarks>
internal sealed class ChezmoiClient(ICommandRunner commands) : IChezmoi
{
    internal const string TokenVariable = "OP_SERVICE_ACCOUNT_TOKEN";
    internal const string StubToken = "render";

    // Prints its last argument: the reference `op read --no-newline <ref>` was given.
    private const string OpStub = "#!/bin/sh\nfor a; do ref=$a; done\nprintf \"%s\" \"$ref\"\n";

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Render(IDirectory source, string profile, IDirectory destination, CancellationToken ct = default)
    {
        var scratch = Directory.CreateTempSubdirectory("lab-chezmoi-");
        try
        {
            var op = Path.Combine(scratch.FullName, "op");
            await File.WriteAllTextAsync(op, OpStub, ct);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(op, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }


            var config = Path.Combine(scratch.FullName, "chezmoi.toml");
            await File.WriteAllTextAsync(config, $"[data]\n  profile = \"{profile}\"\n[onepassword]\n  command = \"{op}\"\n  mode = \"service\"\n", ct);

            var archive = Path.Combine(scratch.FullName, "render.tar");
            await commands.Run(
                Command.Create("chezmoi")
                    .WithArguments(
                        "--source", source.AbsolutePath,
                        "--destination", destination.AbsolutePath,
                        "--config", config,
                        "--cache", Path.Combine(scratch.FullName, "cache"),
                        "archive", "--exclude", "externals", "--output", archive)
                    .WithEnvironmentVariables(new Dictionary<string, string> { [TokenVariable] = StubToken })
                    .ThrowOnError(),
                ct);

            // The system tar, not .NET's reader: chezmoi writes headers the latter cannot parse.
            destination.Create();
            await commands.Run(Command.Create("tar").WithArguments("-x", "-C", destination.AbsolutePath, "-f", archive).ThrowOnError(), ct);
        }
        finally
        {
            // rm, not Directory.Delete: chezmoi's HTTP cache files a download under a path
            // longer than .NET will traverse, and the scratch must go whatever it holds.
            await commands.Run(Command.Create("rm").WithArguments("-rf", scratch.FullName), CancellationToken.None);
        }

        // Everything in a home tree is a dotfile, which .NET counts as hidden and skips by default.
        var everything = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.None };
        return [.. Directory.EnumerateFiles(destination.AbsolutePath, "*", everything)
            .Select(file => Path.GetRelativePath(destination.AbsolutePath, file))
            .Order(StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public async Task Update(CancellationToken ct = default) =>
        await commands.Run(Command.Create("chezmoi").WithArguments("update", "--init", "--no-tty").ThrowOnError(), ct);
}
