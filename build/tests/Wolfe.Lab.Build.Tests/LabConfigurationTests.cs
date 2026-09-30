using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly.Registry;
using Ritten.Engine.DryRun;
using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Launchd;
using Wolfe.Lab.Build.Clients.Alerts;
using Wolfe.Lab.Build.Clients.Garage;
using Wolfe.Lab.Build.Clients.Gatus;
using Wolfe.Lab.Build.Clients.Heartbeat;
using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Clients.Packages;

namespace Wolfe.Lab.Build.Tests;

// The shipped appsettings.json, as every client binds it: a section missing, a limit of zero or
// HTTP options the standard handler rejects fails here rather than on a node.
public class LabConfigurationTests
{
    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        var builder = Substitute.For<IWorkflowBuilder>();
        builder.Services.Returns(services);
        builder.Decorators.Returns(new DecoratorRegistry());
        services.AddSingleton(LabConfiguration.Current);
        services.AddSingleton(Substitute.For<IWorkflowLog>());
        services.AddSingleton(Substitute.For<ICommandRunner>());
        builder.AddGarage().AddOllama().AddPackages().AddGatus().AddAgents().AddAlerts().AddHeartbeat();
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("garage.answering")]
    [InlineData("ollama.serving")]
    [InlineData("launchd.unloading")]
    public void Current_ConfiguresEveryWait(string key)
    {
        if (key == LaunchdSupervisor.Unloading && !OperatingSystem.IsMacOS())
        {
            Assert.Skip("launchd's wait is registered on macOS only.");
        }

        Should.NotThrow(() => Services().GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline<bool>(key));
    }

    [Fact]
    public void Current_ConfiguresEveryHttpClient()
    {
        using var services = Services();

        Should.NotThrow(() => services.GetRequiredService<IHttpClientFactory>().CreateClient("packages"));
        Should.NotThrow(() => services.GetRequiredService<IGatus>());
    }

    [Fact]
    public void Current_SaysWhereTheCliReachesOut()
    {
        using var services = Services();

        services.GetRequiredService<IOptions<AlertsOptions>>().Value.Endpoint.ShouldNotBeNull();
        services.GetRequiredService<IOptions<HealthchecksOptions>>().Value.Endpoint.ShouldNotBeNull();
        var packages = services.GetRequiredService<IOptions<GithubOptions>>().Value;
        packages.Releases.ShouldNotBeNull();
        packages.Api.ShouldNotBeNull();
    }

    [Fact]
    public void Current_SaysWhereTheVaultsServiceAccountIs() =>
        LabConfiguration.Current["OnePassword:ServiceAccountTokenFile"].ShouldNotBeNullOrEmpty();

    [Fact]
    public void Load_TakesTheEnvironmentOverTheFile()
    {
        const string variable = LabConfiguration.EnvironmentPrefix + "Ollama__Serving__Limit";
        LabConfiguration.Load(AppContext.BaseDirectory).GetValue<TimeSpan>("Ollama:Serving:Limit").ShouldBe(TimeSpan.FromSeconds(30));

        Environment.SetEnvironmentVariable(variable, "00:02:00");
        try
        {
            LabConfiguration.Load(AppContext.BaseDirectory).GetValue<TimeSpan>("Ollama:Serving:Limit").ShouldBe(TimeSpan.FromMinutes(2));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }
}
