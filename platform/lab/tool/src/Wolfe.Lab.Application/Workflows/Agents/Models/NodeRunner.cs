namespace Wolfe.Lab.Application.Workflows.Agents.Models;

/// <summary>
/// The runner a deploy runs on, as the job was told (<c>--node</c>): what it finds this node's
/// entry in <c>ritten.json</c> by, while a component declares its agents per node there. The
/// catalog's node is the environment's (<c>LAB_NODE</c>).
/// </summary>
/// <param name="Name">The runner's label: <c>MacMini</c>, <c>MacStudio</c>, <c>wolfe-pi5</c>.</param>
public sealed record NodeRunner(string Name);
