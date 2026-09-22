using System.Net;
using Wolfe.Lab.Build.Clients.Gatus;
using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Tests.Clients.Gatus;

public class GatusClientTests
{
    private static readonly ServiceUrl Url = ServiceUrl.From("http://wolfe-pi5.tailf823b8.ts.net:8280/health");

    private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task Health_ReadsTheStatusGatusReports()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "{\"status\":\"UP\"}");

        var health = await new GatusClient(new HttpClient(handler)).Health(Url, TestContext.Current.CancellationToken);

        health.IsUp.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri(Url.Value));
    }

    [Fact]
    public async Task Health_IsNotUpWhenGatusSaysOtherwise()
    {
        var health = await new GatusClient(new HttpClient(new CapturingHandler(HttpStatusCode.OK, "{\"status\":\"DOWN\"}"))).Health(Url, TestContext.Current.CancellationToken);

        health.IsUp.ShouldBeFalse();
        health.Status.ShouldBe("DOWN");
    }

    [Fact]
    public async Task Health_FailsLoudlyWhenTheEndpointDoesNotAnswerOk()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => new GatusClient(new HttpClient(new CapturingHandler(HttpStatusCode.BadGateway, ""))).Health(Url, TestContext.Current.CancellationToken));
    }
}
