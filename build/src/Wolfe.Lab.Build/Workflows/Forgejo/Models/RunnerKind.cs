namespace Wolfe.Lab.Build.Workflows.Forgejo.Models;

/// <summary>
/// The two runners a node can run.
/// </summary>
public enum RunnerKind
{
    /// <summary>
    /// Steps run in a shell on the node, so only the lab may use it; the node's name is its label.
    /// </summary>
    Host,

    /// <summary>
    /// Jobs run in fresh containers, instance-wide, under a role label every node shares, so a
    /// build lands wherever one is idle.
    /// </summary>
    Docker
}
