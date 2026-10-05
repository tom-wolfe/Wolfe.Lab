namespace Wolfe.Lab.Domain.Network;

/// <summary>
/// Where a machine is reached: a DNS name or an IP address — <c>macmini.tailf823b8.ts.net</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct HostName
{
    private static Validation Validate(string input) =>
        Uri.CheckHostName(input) is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6
            ? Validation.Ok
            : Validation.Invalid(NetworkErrors.NotAHostName(input).Message);
}
