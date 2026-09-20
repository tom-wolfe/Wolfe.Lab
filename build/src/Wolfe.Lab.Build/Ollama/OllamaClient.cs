using Wolfe.Lab.Build.Ollama.Models;

namespace Wolfe.Lab.Build.Ollama;

/// <summary>
/// Runs the ollama binary on the node. Both commands talk to the running server, so a deploy
/// that reaches here has already converged the agent — and a server that is up but not
/// answering fails the job loudly rather than silently serving nothing.
/// </summary>
internal sealed class OllamaClient(ICommandRunner commands) : IOllama
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OllamaModel>> Installed(CancellationToken ct = default)
    {
        var result = await commands.Run(
            Command.Create("ollama").WithArguments("list").QuietOutput().ThrowOnError(), ct);

        return [.. Parse(result.StandardOutput)];
    }

    /// <inheritdoc />
    public async Task Pull(OllamaModel model, CancellationToken ct = default) =>
        await commands.Run(Command.Create("ollama").WithArguments("pull", model.Value).ThrowOnError(), ct);

    /// <summary>
    /// `ollama list` prints a table: NAME, ID, SIZE, MODIFIED. Only the first column is wanted,
    /// and only rows that name a tagged model — which skips the header without matching on it.
    /// </summary>
    internal static IEnumerable<OllamaModel> Parse(string output) =>
        output.Split('\n')
            .Select(line => OllamaModel.TryParse(line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()))
            .OfType<OllamaModel>();
}
