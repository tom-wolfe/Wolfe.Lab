using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Caddy.Models;

namespace Wolfe.Lab.Build.Tests.Workflows.Caddy.Models;

public class CertificateSettingsTests
{
    private static readonly CertificateSettings Complete = new()
    {
        Image = "goacme/lego:v5.4.0",
        Email = "tom@example.com",
        Domains = ["*.twolfe.dev"],
        Dns = "netlify",
        Store = HostPath.From("~/Docker/caddy/lego")
    };

    [Fact]
    public void ToRequest_ReadsACompleteSection()
    {
        var request = Complete.ToRequest().ShouldNotBeNull();

        request.Image.ShouldBe("goacme/lego:v5.4.0");
        request.Domains.ShouldBe(["*.twolfe.dev"]);
        request.PropagationWait.ShouldBeNull();
        request.Store.AbsolutePath.ShouldBe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Docker", "caddy", "lego"));
    }

    [Fact]
    public void ToRequest_IsNullWhileAnythingRequiredIsMissing()
    {
        (Complete with { Image = null }).ToRequest().ShouldBeNull();
        (Complete with { Email = "" }).ToRequest().ShouldBeNull();
        (Complete with { Domains = [] }).ToRequest().ShouldBeNull();
        (Complete with { Dns = null }).ToRequest().ShouldBeNull();
        (Complete with { Store = null }).ToRequest().ShouldBeNull();
    }
}
