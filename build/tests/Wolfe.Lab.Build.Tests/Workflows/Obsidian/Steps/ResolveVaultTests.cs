using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Values;
using Wolfe.Lab.Build.Workflows.Obsidian.Models;
using Wolfe.Lab.Build.Workflows.Obsidian.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Obsidian.Steps;

public class ResolveVaultTests : IDisposable
{
    private readonly DirectoryInfo _checkout = Directory.CreateTempSubdirectory("lab-vault-");

    private KnownVaults Known => new(new Dictionary<string, Vault>
    {
        ["main"] = new("main", new PhysicalDirectory(_checkout.FullName), RepositoryUrl.From("http://forgejo/obsidian-main.git")),
        ["dnd"] = new("dnd", new PhysicalDirectory(Path.Combine(_checkout.FullName, "missing")), RepositoryUrl.From("http://forgejo/obsidian-dnd.git"))
    });

    public void Dispose() => _checkout.Delete(recursive: true);

    [Fact]
    public void Run_ProducesTheNamedVault()
    {
        var result = new ResolveVault(Known, new RequestedVault("main")).Run();

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Value.ShouldNotBeNull().Repository.ShouldBe(RepositoryUrl.From("http://forgejo/obsidian-main.git"));
    }

    [Fact]
    public void Run_NamesTheKnownVaultsWhenTheNameIsUnknown()
    {
        var result = new ResolveVault(Known, new RequestedVault("work")).Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("main, dnd");
    }

    [Fact]
    public void Run_FailsWhenTheCheckoutIsMissing()
    {
        var result = new ResolveVault(Known, new RequestedVault("dnd")).Run();

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("RUNBOOK");
    }
}
