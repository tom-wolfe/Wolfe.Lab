using Polly.Registry;
using Wolfe.Lab.Build.Clients.Resilience;

namespace Wolfe.Lab.Build.Clients.Ollama;

/// <summary>
/// Runs the ollama binary on the node. Both commands talk to the running server, so a deploy
/// that reaches here has already converged the agent — and a server that is up but not
/// answering fails the job loudly rather than silently serving nothing.
/// </summary>
internal sealed class OllamaClient(ICommandRunner commands, ResiliencePipelineProvider<string> pipelines) : IOllama
{
    /// <summary>
    /// The wait for a server to answer, configured under <c>Ollama:Serving</c>.
    /// </summary>
    internal const string Serving = "ollama.serving";

    /// <inheritdoc />
    public async Task<IReadOnlyList<OllamaModel>> Installed(CancellationToken ct = default)
    {
        var result = await commands.Run(
            Command.Create("ollama").WithArguments("list").QuietOutput().ThrowOnError(), ct);

        return [.. Parse(result.StandardOutput)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<OllamaModel, string>> Identities(CancellationToken ct = default)
    {
        var result = await commands.Run(
            Command.Create("ollama").WithArguments("list").QuietOutput().ThrowOnError(), ct);

        return ParseIdentities(result.StandardOutput);
    }

    /// <inheritdoc />
    public async Task Copy(OllamaModel source, OllamaModel destination, CancellationToken ct = default) =>
        await commands.Run(Command.Create("ollama").WithArguments("cp", source.Value, destination.Value).QuietOutput().ThrowOnError(), ct);

    /// <inheritdoc />
    public async Task Remove(OllamaModel model, CancellationToken ct = default) =>
        await commands.Run(Command.Create("ollama").WithArguments("rm", model.Value).QuietOutput().ThrowOnError(), ct);

    /// <inheritdoc />
    public async Task<bool> IsServing(CancellationToken ct = default) =>
        (await commands.Run(Command.Create("ollama").WithArguments("list").QuietOutput(), ct)).ExitCode == 0;

    /// <inheritdoc />
    public async Task<bool> AwaitServing(CancellationToken ct = default) =>
        await pipelines.GetPipeline<bool>(Serving).Until(IsServing, ct: ct);

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

    /// <summary>
    /// The same table, keeping the ID column beside each name.
    /// </summary>
    internal static IReadOnlyDictionary<OllamaModel, string> ParseIdentities(string output) =>
        output.Split('\n')
            .Select(line => line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 2)
            .SelectMany(IEnumerable<KeyValuePair<OllamaModel, string>> (columns) =>
                OllamaModel.TryParse(columns[0]) is { } model ? [KeyValuePair.Create(model, columns[1])] : [])
            .ToDictionary();
}
