using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.Caddy.Models;

namespace Wolfe.Lab.Build.Workflows.Caddy.Steps;

/// <summary>
/// Issues or renews the certificate: lego's <c>run</c> is convergent, so this registers and
/// issues when nothing exists, renews when the ARI window says it is due, and does nothing
/// otherwise — safe on any schedule.
/// </summary>
/// <remarks>
/// Two things about the container are load-bearing. State mounts at <c>/state</c>, not
/// <c>/lego</c>, because the v5 image ships its binary at <c>/lego</c> and a mount there fails
/// with a misleading "not a directory". And the propagation wait replaces lego's own DNS check,
/// because under Docker Desktop every port-53 query goes through the VM's proxy, which serves
/// stale TXT records and sometimes REFUSED; Let's Encrypt validates from its own resolvers, where
/// Netlify's fleet propagates in seconds, so a fixed wait is deterministic where the check is not.
/// </remarks>
[Step("issue certificate", StepKind.Publish)]
internal sealed class IssueCertificate(IDocker docker, ISecretProvider secrets, CertificateRequest request, IWorkflowLog log)
{
    internal const string MountPoint = "/state";

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var environment = new Dictionary<string, string>();
        foreach (var (name, value) in request.Environment)
        {
            environment[name] = await secrets.Resolve(value, ct);
        }

        var run = new ContainerRun(
            request.Image,
            [
                "--log.format", "text",
                "run",
                "--accept-tos",
                "--email", request.Email,
                "--dns", request.Dns,
                .. request.Domains.SelectMany(domain => new[] { "--domains", domain }),
                "--path", MountPoint,
                .. request.PropagationWait is { } wait ? new[] { "--dns.propagation.wait", wait } : []
            ])
        {
            Mounts = [new BindMount(request.Store, MountPoint)],
            Environment = environment
        };

        log.Status($"Issuing or renewing {string.Join(", ", request.Domains)} through {request.Dns}.");
        await docker.Run(run, ct);
        return StepResult.Successful;
    }
}
