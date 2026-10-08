using Ritten.Engine.FileSystem;
using Ritten.Git;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Obsidian.Models;
using Wolfe.Lab.Application.Workflows.Obsidian.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Obsidian;
using Wolfe.Lab.Domain.Git;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Domain.Secrets;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Obsidian.Steps;

public class ResolveVaultTests : IDisposable
{
    private const string Checkout = "/checkout";

    private static readonly SecretReference Token = SecretReference.From("op://Wolfe.Lab/forgejo-obsidian-token/credential");

    private readonly DirectoryInfo _vaults = Directory.CreateTempSubdirectory("lab-vault-");
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ResolveVaultTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(new PhysicalDirectory(Checkout));
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory($"{Checkout}/personal/obsidian/main"));
    }

    public void Dispose() => _vaults.Delete(recursive: true);

    private ObsidianOptions Legacy => new()
    {
        Push = new PushOptions { Username = GitUsername.From("tom-wolfe"), Token = Token },
        Vaults = new()
        {
            ["main"] = new VaultOptions { Path = HostPath.From(_vaults.FullName), Repository = RepositoryUrl.From("http://forgejo/obsidian-main.git") },
            ["dnd"] = new VaultOptions { Path = HostPath.From(Path.Combine(_vaults.FullName, "missing")), Repository = RepositoryUrl.From("http://forgejo/obsidian-dnd.git") }
        },
        Exclude = [".DS_Store"]
    };

    [Fact]
    public async Task Run_IsTheVaultTheDirectoryDeclares()
    {
        var catalog = new ServiceCatalog();
        var service = Catalogs.AddService(catalog, "personal/obsidian").Value.ShouldNotBeNull();
        var vault = ObsidianComponent.Create(new DocumentSource(RepositoryPath.From("personal/obsidian/main/component.yaml")), ComponentName.From("main"), ComponentKind.Backup,
            HostPath.From(_vaults.FullName), RepositoryUrl.From("http://forgejo/declared.git"), new PushCredential(GitUsername.From("tom-wolfe"), Token)).Value.ShouldNotBeNull();
        vault.Exclude = [".trash/"];
        service.Add(vault).Value.ShouldNotBeNull();

        var result = (await Resolve(catalog, "")).Value.ShouldNotBeNull();

        result.Name.ShouldBe("main");
        result.Repository.ShouldBe(RepositoryUrl.From("http://forgejo/declared.git"));
        result.Excludes.ShouldBe([".trash/"]);
    }

    [Fact]
    public async Task Run_IsTheNamedVaultOfRittenJsonWhileNoneIsDeclared()
    {
        var result = (await Resolve(new ServiceCatalog(), "main")).Value.ShouldNotBeNull();

        result.Repository.ShouldBe(RepositoryUrl.From("http://forgejo/obsidian-main.git"));
        result.Push.Username.ShouldBe(GitUsername.From("tom-wolfe"));
        result.Excludes.ShouldBe([".DS_Store"]);
    }

    [Fact]
    public async Task Run_NamesTheKnownVaultsWhenTheNameIsUnknown()
    {
        var result = await Resolve(new ServiceCatalog(), "work");

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("main, dnd");
    }

    [Fact]
    public async Task Run_FailsWhenTheCheckoutIsMissing()
    {
        var result = await Resolve(new ServiceCatalog(), "dnd");

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("RUNBOOK");
    }

    private Task<StepResult<Vault>> Resolve(ServiceCatalog catalog, string requested) =>
        new ResolveVault(new DeclaredComponents(_git, _fileSystem), Legacy, new RequestedVault(requested)).Run(catalog, TestContext.Current.CancellationToken);
}
