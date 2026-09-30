using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Wolfe.Lab.Clients.Resilience;

/// <summary>
/// Registers the lab's resilience pipelines.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds a <see cref="Polling"/> pipeline under <paramref name="key"/>, its limit and interval
        /// read from <paramref name="section"/> of the configuration, for a client to wait with
        /// through <see cref="Polly.Registry.ResiliencePipelineProvider{TKey}"/>. Registered once: a
        /// second client naming the same wait shares it.
        /// </summary>
        public IWorkflowBuilder AddPolling(string key, string section)
        {
            var marker = $"{typeof(Polling).FullName}:{key}";
            if (builder.Services.Any(service => service.ServiceKey as string == marker))
            {
                return builder;
            }

            builder.Services.AddKeyedSingleton(marker, marker);
            builder.Services.AddOptions<PollingOptions>(key).BindConfiguration(section);
            builder.Services.AddResiliencePipeline<string, bool>(key, (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptionsMonitor<PollingOptions>>().Get(key);
                if (options.Limit <= TimeSpan.Zero || options.Interval <= TimeSpan.Zero)
                {
                    throw new InvalidOperationException(
                        $"'{section}' needs a positive Limit and Interval in appsettings.json: waiting on {key} has no limit to give up at.");
                }

                pipeline.TimeProvider = context.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
                pipeline.AddPolling(options.Limit, options.Interval);
            });
            return builder;
        }
    }
}
