using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Wolfe.Lab.Infrastructure.Resilience;

/// <summary>
/// The standard HTTP resilience handler, its options read from a section of <c>appsettings.json</c>.
/// </summary>
public static class HttpResilience
{
    extension(IHttpClientBuilder client)
    {
        /// <summary>
        /// Adds the standard handler — retries, per-attempt and overall timeouts, a breaker — with
        /// <see cref="HttpStandardResilienceOptions"/> bound from <paramref name="section"/>. The
        /// client's own timeout is turned off: the handler owns every limit.
        /// </summary>
        public IHttpClientBuilder AddConfiguredResilience(string section)
        {
            client.ConfigureHttpClient(http => http.Timeout = Timeout.InfiniteTimeSpan);
            var handler = client.AddStandardResilienceHandler();
            client.Services.AddOptions<HttpStandardResilienceOptions>(handler.PipelineName).BindConfiguration(section);
            return client;
        }
    }
}
