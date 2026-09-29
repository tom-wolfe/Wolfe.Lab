using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;
using Wolfe.Lab.Build.Clients.Resilience;

namespace Wolfe.Lab.Build.Tests.Clients.Resilience;

/// <summary>
/// A real pipeline provider with one polling pipeline, registered the way the lab registers them.
/// </summary>
internal static class Pipelines
{
    public static ResiliencePipelineProvider<string> Polling(string key, TimeSpan limit, TimeSpan interval, TimeProvider? time = null)
    {
        var builder = Substitute.For<IWorkflowBuilder>();
        var services = new ServiceCollection();
        builder.Services.Returns(services);
        if (time is not null)
        {
            services.AddSingleton(time);
        }

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"Test:{key}:Limit"] = limit.ToString("c", System.Globalization.CultureInfo.InvariantCulture),
            [$"Test:{key}:Interval"] = interval.ToString("c", System.Globalization.CultureInfo.InvariantCulture)
        }).Build());

        builder.AddPolling(key, $"Test:{key}");
        return services.BuildServiceProvider().GetRequiredService<ResiliencePipelineProvider<string>>();
    }
}
