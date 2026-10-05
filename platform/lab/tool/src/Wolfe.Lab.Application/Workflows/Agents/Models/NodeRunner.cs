namespace Wolfe.Lab.Application.Workflows.Agents.Models;

/// <summary>
/// The runner a deploy runs on, as the job was told (<c>--node</c>): what it finds its node by,
/// in <c>ritten.json</c> or in the catalog.
/// </summary>
/// <param name="Name">The runner's label: <c>MacMini</c>, <c>MacStudio</c>, <c>wolfe-pi5</c>.</param>
public sealed record NodeRunner(string Name);
