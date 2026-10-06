namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// Where compose finds a stack.
/// </summary>
public static class ComposeFiles
{
    /// <summary>
    /// The files compose looks for when none is named, in the order it looks: the compose
    /// specification's, not the lab's. The first it finds is the stack's, an override beside it
    /// merged in.
    /// </summary>
    public static IReadOnlyList<string> Defaults { get; } = ["compose.yaml", "compose.yml", "docker-compose.yaml", "docker-compose.yml"];
}
