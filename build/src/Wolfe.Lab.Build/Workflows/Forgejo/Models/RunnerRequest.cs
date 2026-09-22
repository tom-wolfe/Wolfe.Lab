namespace Wolfe.Lab.Build.Workflows.Forgejo.Models;

/// <summary>
/// What the invocation asked for: which node, which of its runners.
/// </summary>
/// <param name="Node">The node's name, as the runner is named and its vault item is suffixed.</param>
/// <param name="Kind">Which runner.</param>
public sealed record RunnerRequest(string Node, RunnerKind Kind);
