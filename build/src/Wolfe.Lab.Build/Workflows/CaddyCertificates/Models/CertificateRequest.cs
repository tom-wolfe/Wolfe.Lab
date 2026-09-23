namespace Wolfe.Lab.Build.Workflows.CaddyCertificates.Models;

/// <summary>
/// What lego is asked for, as the steps consume it.
/// </summary>
/// <param name="Image">The lego image, tag included.</param>
/// <param name="Email">The ACME account's email.</param>
/// <param name="Domains">The names on the certificate.</param>
/// <param name="Dns">The DNS provider for the challenge.</param>
/// <param name="Environment">The provider's environment, references not yet read.</param>
/// <param name="PropagationWait">How long to wait for the challenge record, or null for lego's own check.</param>
/// <param name="Store">Where lego keeps its state on the node.</param>
public sealed record CertificateRequest(
    string Image,
    string Email,
    IReadOnlyList<string> Domains,
    string Dns,
    IReadOnlyDictionary<string, string> Environment,
    string? PropagationWait,
    IDirectory Store
);
