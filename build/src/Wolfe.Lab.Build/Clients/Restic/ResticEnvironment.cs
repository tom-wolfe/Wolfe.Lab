using Ritten.Engine.FileSystem;

namespace Wolfe.Lab.Build.Clients.Restic;

/// <summary>
/// Reads one of the restic slice's env files into a repository, references resolved.
/// </summary>
/// <remarks>
/// The restic slice publishes the repositories as env files at its root, and every component
/// that snapshots into one reads them from there: the one place a component reads outside its
/// own directory, and on purpose — the repository is the restic slice's to define, and a copy
/// in every backup component would be a copy that drifts. The file is found by walking up from
/// the component to the checkout that holds <c>restic/</c>.
/// </remarks>
internal static class ResticEnvironment
{
    internal const string SliceName = "restic";

    public static async Task<Result<ResticRepository>> Load(IDirectory component, string fileName, ISecretProvider secrets, CancellationToken ct = default)
    {
        if (Find(component, fileName) is not { } file)
        {
            return new Error($"No {SliceName}/{fileName} above {component.AbsolutePath}: the checkout has no restic slice.");
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

    private static IFile? Find(IDirectory from, string fileName)
    {
        for (var directory = from; directory is not null; directory = Parent(directory))
        {
            var candidate = directory.GetDirectory(SliceName).GetFile(fileName);
            if (candidate.Exists)
            {
                return candidate;
            }
        }

        return null;
    }

    private static IDirectory? Parent(IDirectory directory) =>
        Path.GetDirectoryName(directory.AbsolutePath) is { Length: > 0 } parent && parent != directory.AbsolutePath
            ? new PhysicalDirectory(parent)
            : null;
}
