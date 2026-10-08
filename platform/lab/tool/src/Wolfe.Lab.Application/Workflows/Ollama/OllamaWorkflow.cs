using Wolfe.Lab.Application.Workflows.Ollama.Jobs;

namespace Wolfe.Lab.Application.Workflows.Ollama;

/// <summary>
/// The models the lab's servers serve, by what each is used for: <c>"workflow": "ollama"</c>.
/// </summary>
public sealed class OllamaWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "ollama";

    /// <inheritdoc />
    public override string Label => "ollama";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new CheckJob(), new DeployJob()];
}
