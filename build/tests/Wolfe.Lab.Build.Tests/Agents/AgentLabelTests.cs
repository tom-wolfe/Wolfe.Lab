using Wolfe.Lab.Build.Agents.Models;

namespace Wolfe.Lab.Build.Tests.Agents;

public class AgentLabelTests
{
    [Fact]
    public void ForName_PrefixesTheLabWithoutAnyoneSpellingItOut() =>
        AgentLabel.ForName("ollama").ShouldNotBeNull().Value.ShouldBe("dev.twolfe.ollama");

    [Fact]
    public void Name_IsTheKeyItWasDeclaredUnder() =>
        AgentLabel.ForName("forgejo-runner").ShouldNotBeNull().Name.ShouldBe("forgejo-runner");

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("has/slash")]
    [InlineData("dotted.name")]
    public void ForName_RefusesAKeyThatCannotBeALabel(string name) =>
        AgentLabel.ForName(name).ShouldBeNull();
}
