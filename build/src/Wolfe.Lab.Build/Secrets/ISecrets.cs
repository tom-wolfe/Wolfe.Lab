namespace Wolfe.Lab.Build.Secrets;

/// <summary>
/// The one door to the vault: reads a secret by reference, at the moment a step needs it.
/// </summary>
public interface ISecrets
{
    /// <summary>
    /// Reads the secret the reference names.
    /// </summary>
    /// <param name="reference">The secret to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<string> Read(SecretReference reference, CancellationToken cancellationToken = default);
}
