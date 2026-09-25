using Wolfe.Lab.Build.Workflows.Ollama.Models;
using OllamaModel = Wolfe.Lab.Build.Clients.Ollama.OllamaModel;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama;

public class RoleRulesTests
{
    private static ModelSettings Models(string[] pull, params (string Name, string Model, bool Identical)[] roles) => new()
    {
        Pull = [.. pull.Select(OllamaModel.From)],
        Roles = roles.ToDictionary(r => r.Name, r => new ModelRole { Model = OllamaModel.From(r.Model), Identical = r.Identical })
    };

    [Fact]
    public void Local_AcceptsARoleOnAPulledModel() =>
        RoleRules.Local(Models(["qwen3:8b"], ("background", "qwen3:8b", false))).ShouldBeEmpty();

    [Fact]
    public void Local_RefusesARoleOnAModelTheNodeWasNotToldToHold() =>
        RoleRules.Local(Models(["qwen3:8b"], ("background", "qwen3:30b-a3b", false)))
            .ShouldHaveSingleItem().ShouldContain("does not declare");

    [Theory]
    [InlineData("Background")]
    [InlineData("lab/background")]
    [InlineData("back ground")]
    [InlineData("-background")]
    public void Local_RefusesANameThatCannotBeAnAlias(string name) =>
        RoleRules.Local(Models(["qwen3:8b"], (name, "qwen3:8b", false)))
            .ShouldHaveSingleItem().ShouldContain("cannot name a role");

    [Fact]
    public void Local_RefusesARoleWithNoModel() =>
        RoleRules.Local(new ModelSettings { Roles = new Dictionary<string, ModelRole> { ["background"] = new() } })
            .ShouldHaveSingleItem().ShouldContain("names no 'model'");

    [Fact]
    public void Across_LetsABestFitRoleDifferBetweenServers() =>
        RoleRules.Across(new Dictionary<string, ModelSettings>
        {
            ["server"] = Models(["qwen3:8b"], ("background", "qwen3:8b", false)),
            ["studio"] = Models(["qwen3:30b-a3b"], ("background", "qwen3:30b-a3b", false))
        }).ShouldBeEmpty();

    [Fact]
    public void Across_AcceptsAnIdenticalRoleThatIsIdentical() =>
        RoleRules.Across(new Dictionary<string, ModelSettings>
        {
            ["server"] = Models(["embeddinggemma:300m"], ("embedding", "embeddinggemma:300m", true)),
            ["studio"] = Models(["embeddinggemma:300m"], ("embedding", "embeddinggemma:300m", true))
        }).ShouldBeEmpty();

    [Fact]
    public void Across_RefusesAnIdenticalRoleOnDifferentModels() =>
        RoleRules.Across(new Dictionary<string, ModelSettings>
        {
            ["server"] = Models(["embeddinggemma:300m"], ("embedding", "embeddinggemma:300m", true)),
            ["studio"] = Models(["nomic-embed-text:v1.5"], ("embedding", "nomic-embed-text:v1.5", true))
        }).ShouldHaveSingleItem().ShouldContain("studio: nomic-embed-text:v1.5");

    [Fact]
    public void Across_RefusesAnIdenticalRoleAServerDoesNotDeclare() =>
        RoleRules.Across(new Dictionary<string, ModelSettings>
        {
            ["server"] = Models(["embeddinggemma:300m"], ("embedding", "embeddinggemma:300m", true)),
            ["studio"] = Models(["qwen3:8b"])
        }).ShouldHaveSingleItem().ShouldContain("studio does not declare it");

    [Fact]
    public void Across_RefusesABestFitRoleOnlyOneServerDeclares() =>
        // The Studio alone declaring a role is "not found" every evening it sleeps.
        RoleRules.Across(new Dictionary<string, ModelSettings>
        {
            ["server"] = Models(["qwen3:8b"]),
            ["studio"] = Models(["qwen3:30b-a3b"], ("interactive", "qwen3:30b-a3b", false))
        }).ShouldHaveSingleItem().ShouldContain("server does not declare it");

    [Fact]
    public void Across_RefusesAnIdenticalRoleOnlyOneSideMarks() =>
        // Otherwise the rule would be enforced only from the side that remembered to say so.
        RoleRules.Across(new Dictionary<string, ModelSettings>
        {
            ["server"] = Models(["embeddinggemma:300m"], ("embedding", "embeddinggemma:300m", true)),
            ["studio"] = Models(["embeddinggemma:300m"], ("embedding", "embeddinggemma:300m", false))
        }).ShouldHaveSingleItem().ShouldContain("not marked identical");
}
