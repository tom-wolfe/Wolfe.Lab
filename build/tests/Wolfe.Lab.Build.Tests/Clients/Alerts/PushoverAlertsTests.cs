using System.Net;
using Wolfe.Lab.Build.Clients.Alerts;

namespace Wolfe.Lab.Build.Tests.Clients.Alerts;

public class PushoverAlertsTests
{
    private sealed class CapturingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }

    private readonly ISecretProvider _secrets = Substitute.For<ISecretProvider>();

    public PushoverAlertsTests()
    {
        _secrets.Resolve(PushoverAlerts.Token.Value, Arg.Any<CancellationToken>()).Returns("app-token");
        _secrets.Resolve(PushoverAlerts.User.Value, Arg.Any<CancellationToken>()).Returns("user-key");
    }

    [Fact]
    public async Task Send_PostsTheFormAlertShSends()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);

        await new PushoverAlerts(new HttpClient(handler), _secrets).Send(new Alert("obsidian sync failed", "http://run"), TestContext.Current.CancellationToken);

        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(PushoverAlerts.Endpoint);
        // Form encoding spells a space as '+', which UnescapeDataString leaves alone.
        var fields = handler.Body.ShouldNotBeNull().Split('&').Select(f => Uri.UnescapeDataString(f.Replace('+', ' '))).ToList();
        fields.ShouldContain("token=app-token");
        fields.ShouldContain("user=user-key");
        fields.ShouldContain("title=obsidian sync failed");
        fields.ShouldContain("message=http://run");
        fields.ShouldContain("priority=-1");
    }

    [Fact]
    public async Task Send_FailsLoudlyWhenPushoverRefuses()
    {
        var handler = new CapturingHandler(HttpStatusCode.BadRequest);

        await Should.ThrowAsync<HttpRequestException>(
            () => new PushoverAlerts(new HttpClient(handler), _secrets).Send(new Alert("t", "m"), TestContext.Current.CancellationToken));
    }
}
