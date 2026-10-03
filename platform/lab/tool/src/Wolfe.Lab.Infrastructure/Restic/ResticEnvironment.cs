using Wolfe.Lab.Domain;
using Wolfe.Lab.Infrastructure.Checkout;

namespace Wolfe.Lab.Infrastructure.Restic;

/// <summary>
/// Reads one of the restic service's env files into a repository, references resolved.
/// </summary>
/// <remarks>
/// The restic service publishes the repositories as env files at its root, and every component
/// that snapshots into one reads them from there: the one place a component reads outside its
/// own directory, and on purpose — the repository is the restic service's to define, and a copy
/// in every backup component would be a copy that drifts (<see cref="Service"/>).
/// </remarks>
public static class ResticEnvironment
{
    /// <summary>
    /// The repository an env file of the restic service names, its references resolved — or why
    /// it cannot be had.
    /// </summary>
    /// <param name="component">The component reading it, where the walk up to the restic service starts.</param>
    /// <param name="fileName">The env file: the local repository's, or the offsite one's.</param>
    /// <param name="secrets">What resolves the file's references.</param>
    /// <param name="ct">A token to monitor for cancellation.</param>
    public static async Task<Result<ResticRepository>> Load(IDirectory component, string fileName, ISecretProvider secrets, CancellationToken ct = default)
    {
        if (Service.Restic.FindFile(component, fileName) is not { } file || await file.ReadAllTextIfExists(ct) is not { } text)
        {
            return new Error($"No {Service.Restic}/{fileName} above {component.AbsolutePath}: the checkout has no restic service.");
        }

        if (!EnvironmentFile.Parse(text, file.AbsolutePath).TryGetValue(out var entries, out var errors))
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
