using Wolfe.Lab.Application.Workflows.Agents.Steps;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Tests.Application.Agents;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Agents.Steps;

public class CheckAgentDeclarationsTests
{
    private const string Directory = "monitoring/beszel/agent";

    private static StepResult Check(ServiceCatalog catalog) =>
        new CheckAgentDeclarations(Resolvers.On(), new WorkflowJob("agents", "check", DryRun: false, AutoApprove: false), Substitute.For<IWorkflowLog>())
            .Run(catalog, catalog.DeploymentUnitAt(RepositoryPath.From(Directory)).ShouldNotBeNull());

    private static AgentProcess Beszel(params (string Variable, string Value)[] environment) => new()
    {
        Name = AgentName.From("beszel-agent"),
        Program = Template.From("{package}/beszel-agent"),
        Environment = Catalogs.Variables(environment)
    };

    private static StepResult Check(AgentProcess agent, params Node[] nodes) =>
        Check(Catalogs.Of(Catalogs.Nodes(nodes), Directory, Catalogs.Agent("agent", DeploymentTarget.All, agent)));

    [Fact]
    public void Run_PassesAnAgentOnEveryNodeItRunsOn() =>
        Check(Beszel(("DOCKER_HOST", "{node.docker}")),
                Catalogs.Node("mini", NodeRole.Server, docker: "unix:///var/run/docker.sock"),
                Catalogs.Node("pi", NodeRole.Server, NodePlatform.LinuxArm64, docker: "unix:///var/run/docker.sock"))
            .IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_RefusesAPlaceholderANodeHasNothingFor_NamingTheNode()
    {
        var result = Check(Beszel(("DOCKER_HOST", "{node.docker}")),
            Catalogs.Node("mini", NodeRole.Server, docker: "unix:///var/run/docker.sock"), Catalogs.Node("studio", NodeRole.Hybrid));

        var error = result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message;
        error.ShouldStartWith("studio: ");
        error.ShouldContain("{node.docker}");
    }

    [Fact]
    public void Run_RefusesAVaultReferenceThatIsNotOne() =>
        Check(Beszel(("TOKEN", "op://Wolfe.Lab/beszel-agent")), Catalogs.Node("mini", NodeRole.Server))
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldBe("mini: agent 'beszel-agent' sets TOKEN to 'op://Wolfe.Lab/beszel-agent', which is not op://<vault>/<item>/<field>.");

    [Fact]
    public void Run_RefusesAHomePathInTheEnvironment() =>
        Check(Beszel(("DATA", "~/.local/share/thing")), Catalogs.Node("studio", NodeRole.Hybrid))
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("DATA");

    [Fact]
    public void Run_NamesTheNodeOfEveryProblem() =>
        Check(Beszel(("TOKEN", "op://Wolfe.Lab/beszel-agent")), Catalogs.Node("mini", NodeRole.Server), Catalogs.Node("pi", NodeRole.Server))
            .Errors.ShouldNotBeNull().Select(e => e.Message.Split(':')[0]).ShouldBe(["mini", "pi"]);

    [Fact]
    public void Run_RefusesAComponentThatDeclaresNoAgent() =>
        Check(Catalogs.Of(Directory, Catalogs.Definition("agent", ComponentKind.Collector, WorkflowName.Agents)))
            .Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("declares no agent");
}
