using System.Text.Json;
using System.Text.Json.Nodes;
using Wolfe.Lab.Domain.Catalog.Components;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Which document each workflow's components are written as: <c>workflow</c> is the
/// discriminator, so a workflow that reads more than every component declares is read, and
/// judged, as its own shape.
/// </summary>
internal static class ComponentDocuments
{
    private static readonly Dictionary<WorkflowName, Type> Shapes = new()
    {
        [WorkflowName.Docker] = typeof(DockerDocument),
        [WorkflowName.DotNetService] = typeof(DockerDocument),
        [WorkflowName.Agent] = typeof(AgentDocument),
        [WorkflowName.Ollama] = typeof(ModelDocument),
        [WorkflowName.Backup] = typeof(BackupDocument)
    };

    /// <summary>
    /// The document a component <paramref name="workflow"/> operates is written as.
    /// </summary>
    public static Type For(WorkflowName workflow) => Shapes.GetValueOrDefault(workflow, typeof(ComponentDocument));

    /// <summary>
    /// <paramref name="node"/> as the document its <c>workflow</c> says it is.
    /// </summary>
    /// <remarks>The schema has judged the shape already, so this cannot fail on it.</remarks>
    public static ComponentDocument Read(JsonNode? node, JsonSerializerOptions options)
    {
        var workflow = WorkflowName.From(node?["workflow"]?.GetValue<string>() ?? "");
        var shape = IsFormerServer(node) ? typeof(AgentDocument) : For(workflow);
        return node.Deserialize(shape, options) as ComponentDocument
               ?? throw new InvalidOperationException($"A {workflow} component the schema passed did not deserialize.");
    }

    /// <summary>
    /// Whether <paramref name="node"/> is an Ollama server as it was declared before the models
    /// were: <c>workflow: ollama</c>, with an agent. Read as the agent it is, until every server
    /// declares <c>workflow: agent</c>.
    /// </summary>
    public static bool IsFormerServer(JsonNode? node) =>
        node?["workflow"]?.GetValue<string>() == WorkflowName.Ollama.Value && node["agent"] is not null;
}
