using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Deploy.Steps;

/// <summary>
/// Reads every reference the slice's <c>secrets.env</c> names, for the compose invocation.
/// </summary>
[Step("resolve compose secrets", StepKind.Work)]
internal sealed class ResolveComposeSecrets(ISecrets secrets, IWorkflowLog log)
{
    internal const string FileName = "secrets.env";

    public async Task<StepResult<ComposeEnvironment>> Run(Slice slice, CancellationToken ct = default)
    {
        var file = slice.Source.GetFile(FileName);
        if (!file.Exists)
        {
            log.Detail($"{slice.Name} names no secrets.");
            return ComposeEnvironment.Empty;
        }

        using var reader = new StreamReader(file.OpenRead());
        var references = SecretsFile.Parse(await reader.ReadToEndAsync(ct), file.AbsolutePath);
        if (references.IsError)
        {
            return StepResult.Failed(references.Errors!);
        }

        var variables = new Dictionary<string, string>();
        foreach (var (name, reference) in references.Value!)
        {
            variables[name] = await secrets.Read(reference, ct);
        }

        log.Detail($"Resolved {variables.Count} secret{(variables.Count == 1 ? "" : "s")} for compose.");
        return new ComposeEnvironment(variables);
    }
}
