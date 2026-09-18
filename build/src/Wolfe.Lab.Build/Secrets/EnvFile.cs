namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// An env file the way <c>op run --env-file</c> reads one: <c>NAME=value</c> per line, where a
/// value is either an <c>op://</c> reference to resolve or a literal to pass through. What
/// <c>restic/*.env</c> are — a repository path beside a vaulted password. <see cref="SecretsFile"/>
/// is the stricter reading for a file that must hold references only.
/// </summary>
public static class EnvFile
{
    /// <summary>
    /// Reads the file's entries, keyed by variable name.
    /// </summary>
    /// <param name="text">The file's contents.</param>
    /// <param name="fileName">The file, for the error.</param>
    public static Result<IReadOnlyDictionary<string, EnvValue>> Parse(string text, string fileName)
    {
        var entries = new Dictionary<string, EnvValue>();
        var errors = new List<Error>();
        foreach (var (line, number) in text.Split('\n').Select((line, index) => (line.Trim(), index + 1)))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                errors.Add(new Error($"{fileName}:{number}: expected NAME=value."));
                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            entries[name] = SecretReference.TryFrom(value, out var reference) ? new EnvValue(null, reference) : new EnvValue(value, null);
        }

        return errors.Count > 0 ? errors : entries;
    }

    /// <summary>
    /// Resolves every entry to its value: references through the vault, literals as they are.
    /// </summary>
    /// <param name="entries">The parsed entries.</param>
    /// <param name="secrets">The vault.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public static async Task<IReadOnlyDictionary<string, string>> Resolve(this IReadOnlyDictionary<string, EnvValue> entries, ISecrets secrets, CancellationToken ct = default)
    {
        var variables = new Dictionary<string, string>();
        foreach (var (name, value) in entries)
        {
            variables[name] = value.Reference is { } reference ? await secrets.Read(reference, ct) : value.Literal!;
        }

        return variables;
    }
}
