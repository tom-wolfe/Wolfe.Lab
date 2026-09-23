using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Reads every reference the component's <c>secrets.env</c> names, for the compose invocation.
/// </summary>
/// <remarks>
/// A secret never sits in a file: the file holds references, the deploy resolves them into the
/// environment of the one <c>compose up</c> that creates the container, and the container
/// keeps what it was created with. A running stack has no vault in the loop.
/// </remarks>
[Step("resolve compose secrets", StepKind.Work)]
internal sealed class ResolveComposeSecrets(ISecretProvider secrets, IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string FileName = "secrets.env";

    public async Task<StepResult<ComposeEnvironment>> Run(CancellationToken ct = default)
    {
        var file = fileSystem.ProjectRoot.GetFile(FileName);
        if (!file.Exists)
        {
            log.Detail($"{fileSystem.ProjectRoot.Name} names no secrets.");
            return ComposeEnvironment.Empty;
        }

        using var reader = new StreamReader(file.OpenRead());
        if (!EnvironmentFile.Parse(await reader.ReadToEndAsync(ct), file.AbsolutePath).TryGetValue(out var entries, out var errors))
        {
            return StepResult.Failed(errors);
        }

        // Every value must be a reference: a literal here is a secret in the repository, which
        // is the one place a secret must never be, so it is refused before anything is read.
        var literals = entries.Where(entry => !SecretReference.TryFrom(entry.Value, out _)).Select(entry => entry.Key).ToList();
        if (literals.Count > 0)
        {
            return new Error($"{file.AbsolutePath}: {string.Join(", ", literals)} must be op://<vault>/<item>/<field> references.");
        }

        var variables = new Dictionary<string, string>();
        foreach (var (name, reference) in entries)
        {
            variables[name] = await secrets.Resolve(reference, ct);
        }

        log.Detail($"Resolved {variables.Count} secret{(variables.Count == 1 ? "" : "s")} for compose.");
        return new ComposeEnvironment(variables);
    }
}
