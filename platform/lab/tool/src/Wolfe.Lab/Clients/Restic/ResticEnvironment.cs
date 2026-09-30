using Wolfe.Lab.Values;

namespace Wolfe.Lab.Clients.Restic;

/// <summary>
/// Reads one of the restic slice's env files into a repository, references resolved.
/// </summary>
/// <remarks>
/// The restic slice publishes the repositories as env files at its root, and every component
/// that snapshots into one reads them from there: the one place a component reads outside its
/// own directory, and on purpose — the repository is the restic slice's to define, and a copy
/// in every backup component would be a copy that drifts (<see cref="Slice"/>).
/// </remarks>
internal static class ResticEnvironment
{
    public static async Task<Result<ResticRepository>> Load(IDirectory component, string fileName, ISecretProvider secrets, CancellationToken ct = default)
    {
        if (Slice.Restic.FindFile(component, fileName) is not { } file)
        {
            return new Error($"No {Slice.Restic}/{fileName} above {component.AbsolutePath}: the checkout has no restic slice.");
        }

        using var reader = new StreamReader(file.OpenRead());
        if (!EnvironmentFile.Parse(await reader.ReadToEndAsync(ct), file.AbsolutePath).TryGetValue(out var entries, out var errors))
        {
            return errors;
        }

        // Judged before anything is read: a file that names no repository is wrong whatever its
        // password resolves to.
        if (!entries.ContainsKey(ResticRepository.LocationVariable))
        {
            return new Error($"{file.AbsolutePath} does not set {ResticRepository.LocationVariable}.");
        }

        var variables = new Dictionary<string, string>();
        foreach (var (name, value) in entries)
        {
            variables[name] = await secrets.Resolve(value, ct);
        }

        return new ResticRepository(variables);
    }
}
