namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// Reads through the 1Password CLI, authenticated using he service-account token from the environment.
/// </summary>
internal sealed class OnePasswordSecrets(ICommandRunner commands) : ISecrets
{
    private const string TokenVariable = "OP_SERVICE_ACCOUNT_TOKEN";

    /// <inheritdoc />
    public async Task<string> Read(SecretReference reference, CancellationToken cancellationToken = default)
    {
        // The reference is safe to log; the value never is, so both streams are redacted.
        var command = Command.Create("op")
            .WithArguments("read", "--no-newline", reference.Value)
            .RedactOutput()
            .QuietOutput();
        if (ServiceAccountToken() is { } token)
        {
            command = command.WithEnvironmentVariables(new Dictionary<string, string> { [TokenVariable] = token });
        }

        var result = await commands.Run(command, cancellationToken);
        if (result.IsError)
        {
            // Redaction keeps the runner's own failure text to the exit code, which is no help
            // at all. What op writes to stderr names the vault, the item or the account, never
            // the value, and that is the message worth having.
            var why = result.ErrorTail(3);
            throw new CommandFailedException(
                $"Could not read {reference.Value}: {(why.Count == 0 ? $"op exited with code {result.ExitCode}" : string.Join(' ', why))}",
                result);
        }

        return result.StandardOutput;
    }

    private static string? ServiceAccountToken()
    {
        if (Environment.GetEnvironmentVariable(TokenVariable) is { Length: > 0 } fromEnvironment)
        {
            return fromEnvironment;
        }

        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Docker", "1password", "service-account-token");
        return File.Exists(file) && File.ReadAllText(file).Trim() is { Length: > 0 } fromFile ? fromFile : null;
    }
}
