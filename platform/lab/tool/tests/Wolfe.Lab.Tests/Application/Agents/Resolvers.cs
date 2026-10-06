using Microsoft.Extensions.Options;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Application.Agents;

/// <summary>
/// The agent resolver a step under test is given: on <paramref name="node"/>, keeping the lab where
/// the catalog's test nodes do unless a test says otherwise.
/// </summary>
internal static class Resolvers
{
    public static AgentResolver On(string? node = "mini", string root = "/lab/root", string data = "/lab/data") =>
        new(Options.Create(new LabDirectories { Root = new PhysicalDirectory(root), Data = new PhysicalDirectory(data) }), Options.Create(new LabNode { Given = node }));
}
