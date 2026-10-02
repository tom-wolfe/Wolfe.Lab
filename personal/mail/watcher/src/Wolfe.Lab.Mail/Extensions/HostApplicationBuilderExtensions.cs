using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Wolfe.Lab.Mail.Diagnostics;

namespace Wolfe.Lab.Mail.Extensions;

internal static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// The configuration key that names the collector, as OpenTelemetry spells it everywhere.
    /// </summary>
    internal const string CollectorKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Adds traces, metrics and logs, sent to the node's collector.
        /// </summary>
        public IHostApplicationBuilder AddTelemetry()
        {
            // MailKit's own spans and metrics exist only once asked for.
            MailKit.Telemetry.Configure();

            var telemetry = builder.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("mail-watcher"))
                .WithTracing(tracing => tracing
                    .AddSource(
                        MailTelemetry.Name,
                        MailKit.Telemetry.ImapClient.ActivitySourceName,
                        MailKit.Telemetry.SmtpClient.ActivitySourceName)
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                )
                .WithMetrics(metrics => metrics
                    .AddMeter(
                        MailTelemetry.Name,
                        MailKit.Telemetry.ImapClient.MeterName,
                        MailKit.Telemetry.SmtpClient.MeterName,
                        "System.Runtime")
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                )
                .WithLogging(configureBuilder: null, configureOptions: logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                });

            if (builder.Configuration[CollectorKey] is { Length: > 0 })
            {
                telemetry.UseOtlpExporter();
            }

            return builder;
        }
    }
}
