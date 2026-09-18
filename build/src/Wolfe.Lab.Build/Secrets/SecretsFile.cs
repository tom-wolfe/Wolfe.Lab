namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// A slice's <c>secrets.env</c>: one <c>NAME="op://…"</c> per line, comments and blanks
/// ignored. The same file <c>scripts/secrets.sh</c> feeds to <c>op run</c>, read here so a
/// slice keeps one spelling of its secrets whichever path deploys it.
/// </summary>
public static class SecretsFile
{
    /// <summary>
    /// Reads the file's references, keyed by variable name.
    /// </summary>
    /// <param name="text">The file's contents.</param>
    /// <param name="fileName">The file, for the error.</param>
    public static Result<IReadOnlyDictionary<string, SecretReference>> Parse(string text, string fileName)
    {
        var references = new Dictionary<string, SecretReference>();
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
                errors.Add(new Error($"{fileName}:{number}: expected NAME=\"op://…\"."));
                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            if (!SecretReference.TryFrom(value, out var reference))
            {
                errors.Add(new Error($"{fileName}:{number}: '{value}' is not an op://<vault>/<item>/<field> reference."));
                continue;
            }

            references[name] = reference;
        }

        return errors.Count > 0 ? errors : references;
    }
}
