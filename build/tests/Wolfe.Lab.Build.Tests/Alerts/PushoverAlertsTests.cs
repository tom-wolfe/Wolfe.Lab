using System.Net;
using Wolfe.Lab.Build.Alerts;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Alerts;

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

    private readonly ISecrets _secrets = Substitute.For<ISecrets>();

    public PushoverAlertsTests()
    {
        _secrets.Read(PushoverAlerts.Token, Arg.Any<CancellationToken>()).Returns("app-token");
        _secrets.Read(PushoverAlerts.User, Arg.Any<CancellationToken>()).Returns("user-key");
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
