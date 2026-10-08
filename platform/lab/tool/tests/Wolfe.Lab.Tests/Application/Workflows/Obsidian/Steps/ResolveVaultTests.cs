using Wolfe.Lab.Application.Workflows.Obsidian.Steps;
using Wolfe.Lab.Domain.Catalog.Components.Obsidian;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Obsidian.Steps;

public class ResolveVaultTests : IDisposable
{
    private readonly DirectoryInfo _vaults = Directory.CreateTempSubdirectory("lab-vault-");

    public void Dispose() => _vaults.Delete(recursive: true);

    [Fact]
    public void Run_IsTheVaultTheDirectoryDeclares()
    {
        var vault = new ResolveVault().Run(Catalogs.UnitOf<ObsidianComponent>(Catalogs.Vault(_vaults.FullName))).Value.ShouldNotBeNull();

        vault.Name.Value.ShouldBe("main");
        vault.Path.Directory.AbsolutePath.ShouldBe(_vaults.FullName);
    }

    [Fact]
    public void Run_FailsWhenTheCheckoutIsMissing()
    {
        var result = new ResolveVault().Run(Catalogs.UnitOf<ObsidianComponent>(Catalogs.Vault(Path.Combine(_vaults.FullName, "missing"))));

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("RUNBOOK");
    }
}
