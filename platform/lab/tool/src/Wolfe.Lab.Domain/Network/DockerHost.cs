namespace Wolfe.Lab.Domain.Network;

/// <summary>
/// Where a Docker daemon is reached, as <c>DOCKER_HOST</c> names it:
/// <c>unix:///var/run/docker.sock</c>, or a <c>tcp://</c>, <c>ssh://</c> or <c>npipe://</c> endpoint.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct DockerHost
{
    private static readonly string[] Schemes = ["unix", "tcp", "ssh", "npipe"];

    private static Validation Validate(string input) =>
        Uri.TryCreate(input, UriKind.Absolute, out var uri) && Schemes.Contains(uri.Scheme)
            ? Validation.Ok
            : Validation.Invalid(NetworkErrors.NotADockerHost(input, Schemes).Message);
}
