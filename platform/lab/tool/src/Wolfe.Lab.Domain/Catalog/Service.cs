namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Describes a service in the lab (<c>kind: service</c>).
/// </summary>
/// <param name="Name">The service's identifier.</param>
/// <param name="DisplayName">Human-facing name for the service.</param>
/// <param name="Description">A brief sentence describing what the service is for.</param>
/// <param name="Lifecycle">Where it is in its life.</param>
/// <param name="Links">Where to read about it, use it and watch it.</param>
/// <param name="DependsOn">The services this service needs to run.</param>
public sealed record Service(
    ServiceName Name,
    string? DisplayName,
    string Description,
    Lifecycle Lifecycle,
    IReadOnlyList<ServiceLink> Links,
    IReadOnlyList<ServiceName> DependsOn
);
