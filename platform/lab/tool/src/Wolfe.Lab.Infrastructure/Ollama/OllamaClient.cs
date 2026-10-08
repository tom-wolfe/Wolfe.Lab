using Polly.Registry;
using Wolfe.Lab.Domain.Catalog.Components.Models;
using Wolfe.Lab.Infrastructure.Resilience;

namespace Wolfe.Lab.Infrastructure.Ollama;

/// <summary>
/// Runs the ollama binary on the node. Both commands talk to the running server, so a deploy
/// that reaches here has already converged the agent — and a server that is up but not
/// answering fails the job loudly rather than silently serving nothing.
/// </summary>
internal sealed class OllamaClient(ICommandRunner commands, IFileSystem fileSystem, ResiliencePipelineProvider<string> pipelines) : IOllama
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
    public async Task<OllamaModelfile?> Describe(OllamaModel model, CancellationToken ct = default)
    {
        var result = await commands.Run(Command.Create("ollama").WithArguments("show", model.Value, "--modelfile").QuietOutput(), ct);
        return result.ExitCode == 0 ? OllamaModelfile.Parse(result.StandardOutput) : null;
    }

    /// <inheritdoc />
    public async Task Create(OllamaModel name, OllamaModel from, ContextLength? context, CancellationToken ct = default)
    {
        var scratch = fileSystem.CreateTempDirectory("lab-modelfile-");
        try
        {
            var modelfile = scratch.GetFile("Modelfile");
            await modelfile.WriteAllText(Modelfile(from, context), cancellationToken: ct);
            await commands.Run(Command.Create("ollama").WithArguments("create", name.Value, "-f", modelfile.AbsolutePath).QuietOutput().ThrowOnError(), ct);
        }
        finally
        {
            scratch.Delete();
        }
    }

    /// <summary>
    /// The Modelfile of a model built from <paramref name="from"/>, running with <paramref name="context"/> when given.
    /// </summary>
    internal static string Modelfile(OllamaModel from, ContextLength? context) =>
        context is { } length ? $"FROM {from.Value}\nPARAMETER num_ctx {length.Value}\n" : $"FROM {from.Value}\n";

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
}
