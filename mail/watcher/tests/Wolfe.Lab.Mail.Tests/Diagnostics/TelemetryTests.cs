using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Wolfe.Lab.Mail.Diagnostics;
using Wolfe.Lab.Mail.Extensions;

namespace Wolfe.Lab.Mail.Tests.Diagnostics;

public class TelemetryTests
{
    [Fact]
    public void AddTelemetry_RecordsTheWatchersSpansAndMailKits()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.AddTelemetry();
        using var host = builder.Build();

        // The tracer provider is what subscribes; until something resolves it, nothing listens.
        host.Services.GetRequiredService<TracerProvider>();

        using var watcher = MailTelemetry.Source.StartActivity("probe");
        watcher.ShouldNotBeNull("the watcher's own spans are not being recorded.");

        using var smtp = new ActivitySource(MailKit.Telemetry.SmtpClient.ActivitySourceName).StartActivity("probe");
        smtp.ShouldNotBeNull("MailKit's SMTP spans are not being recorded.");
    }

    [Fact]
    public void AddChatClient_ReportsEveryModelCall()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Model:Endpoint"] = "http://127.0.0.1:1",
                ["Model:Name"] = "lab/background"
            })
            .Build());
        services.AddChatClient();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IChatClient>().GetService<OpenTelemetryChatClient>()
            .ShouldNotBeNull("the model client has no OpenTelemetry in its pipeline, so a model call would go unmeasured.");
    }
}
