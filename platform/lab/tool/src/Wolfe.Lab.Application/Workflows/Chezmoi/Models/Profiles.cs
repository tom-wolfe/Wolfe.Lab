using Wolfe.Lab.Domain.Catalog.Components.Chezmoi;

namespace Wolfe.Lab.Application.Workflows.Chezmoi.Models;

/// <summary>
/// The profiles a check renders.
/// </summary>
/// <param name="Names">Their names.</param>
public sealed record Profiles(IReadOnlyList<ChezmoiProfile> Names);
