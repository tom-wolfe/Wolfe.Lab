using System.Net;
using Wolfe.Lab.Build.Clients.Heartbeat;
using Wolfe.Lab.Build.Clients.Secrets;

namespace Wolfe.Lab.Build.Tests.Clients.Heartbeat;

public class HealthchecksHeartbeatTests
{
    private sealed class CapturingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private static readonly HeartbeatCheck Check = new("lab-restic-offsite", SecretReference.From("op://Wolfe.Lab/healthchecks-ping-key/credential"));
    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    public HealthchecksHeartbeatTests() => _secrets.Resolve(Check.Key.Value, Arg.Any<CancellationToken>()).Returns("ping-key");

    [Fact]
    public async Task Ping_GetsTheCheckBySlugUnderTheKey()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);

        await new HealthchecksHeartbeat(new HttpClient(handler), _secrets).Ping(Check, TestContext.Current.CancellationToken);

        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://hc-ping.com/ping-key/lab-restic-offsite"));
    }

    [Fact]
    public async Task Ping_FailsLoudlyWhenTheCheckIsUnknown()
    {
        var handler = new CapturingHandler(HttpStatusCode.NotFound);

        await Should.ThrowAsync<HttpRequestException>(
            () => new HealthchecksHeartbeat(new HttpClient(handler), _secrets).Ping(Check, TestContext.Current.CancellationToken));
    }
}
