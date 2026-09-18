using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Restic;

/// <summary>
/// The restic slice's env files, read from any slice in the checkout: slices are siblings, and
/// <c>restic/</c> is the one holding the repository definitions.
/// </summary>
internal static class ResticEnvironment
{
    internal const string SliceName = "restic";

    /// <summary>
    /// Loads one env file into a repository, every reference resolved.
    /// </summary>
    /// <param name="slice">The slice the job runs in.</param>
    /// <param name="fileName">The env file in <c>restic/</c>.</param>
    /// <param name="secrets">The vault.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public static async Task<Result<ResticRepository>> Load(Slice slice, string fileName, ISecrets secrets, CancellationToken ct = default)
    {
        var file = new PhysicalDirectory(Path.Combine(slice.Source.AbsolutePath, "..", SliceName)).GetFile(fileName);
        if (!file.Exists)
        {
            return new Error($"{file.AbsolutePath} does not exist: the checkout has no restic slice beside {slice.Name}.");
        }

        using var reader = new StreamReader(file.OpenRead());
        if (!EnvFile.Parse(await reader.ReadToEndAsync(ct), file.AbsolutePath).TryGetValue(out var entries, out var errors))
        {
            return errors;
        }

        if (!entries.ContainsKey(ResticRepository.LocationVariable))
        {
            return new Error($"{file.AbsolutePath} does not set {ResticRepository.LocationVariable}.");
        }

        return new ResticRepository(await entries.Resolve(secrets, ct));
    }
}
