using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfe.Lab.Mail.Services.Ai;
using Wolfe.Lab.Mail.Tests.Fakes;

namespace Wolfe.Lab.Mail.Tests.Detection;

/// <summary>
/// One client over the models, best first: the next is asked only when the endpoint does not have
/// the model — the Studio asleep, the mini answering — and never to hide an outage.
/// </summary>
public class FallbackChatClientTests
{
    private static readonly ChatMessage[] Question = [new(ChatRole.User, "Barber on the 24th at 2:30pm.")];

    private static FallbackChatClient Client(params (string Name, IChatClient Client)[] models) =>
        new([.. models.Select(m => new ChatModel(m.Name, m.Client))], NullLogger<FallbackChatClient>.Instance);

    [Fact]
    public async Task GetResponse_AsksTheNextModelWhenTheEndpointDoesNotHaveOne()
    {
        var client = Client(
            ("qwen3:30b-a3b", new BrokenChatClient(Endpoint.ModelNotFound("qwen3:30b-a3b"))),
            ("qwen3:8b", new CannedChatClient("from the mini")));

        var response = await client.GetResponseAsync(Question, ct: TestContext.Current.CancellationToken);

        response.Text.ShouldBe("from the mini");
    }

    [Fact]
    public async Task GetResponse_AsksOnlyTheBestModelWhenItAnswers()
    {
        var fallback = new CannedChatClient("from the mini");
        var client = Client(("qwen3:30b-a3b", new CannedChatClient("from the Studio")), ("qwen3:8b", fallback));

        var response = await client.GetResponseAsync(Question, ct: TestContext.Current.CancellationToken);

        response.Text.ShouldBe("from the Studio");
        fallback.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task GetResponse_DoesNotHideAnOutageBehindTheNextModel()
    {
        var fallback = new CannedChatClient("from the mini");
        var client = Client(("qwen3:30b-a3b", new BrokenChatClient(Endpoint.Down())), ("qwen3:8b", fallback));

        await Should.ThrowAsync<HttpRequestException>(() =>
            client.GetResponseAsync(Question, ct: TestContext.Current.CancellationToken));
        fallback.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task GetResponse_ThrowsTheLastNotFoundWhenNoModelIsServed()
    {
        var client = Client(
            ("qwen3:30b-a3b", new BrokenChatClient(Endpoint.ModelNotFound("qwen3:30b-a3b"))),
            ("qwen3:8b", new BrokenChatClient(Endpoint.ModelNotFound("qwen3:8b"))));

        var thrown = await Should.ThrowAsync<ClientResultException>(() =>
            client.GetResponseAsync(Question, ct: TestContext.Current.CancellationToken));
        thrown.Message.ShouldContain("qwen3:8b");
    }

    [Fact]
    public async Task GetStreamingResponse_FallsBackBeforeTheFirstUpdate()
    {
        var client = Client(
            ("qwen3:30b-a3b", new BrokenChatClient(Endpoint.ModelNotFound("qwen3:30b-a3b"))),
            ("qwen3:8b", new CannedChatClient("from the mini")));

        var words = await Stream(client);

        words.ShouldBe(["from", "the", "mini"]);
    }

    [Fact]
    public async Task GetStreamingResponse_NeverSwitchesModelsMidAnswer()
    {
        // A model that has started answering has been chosen: splicing on another model's answer
        // would be worse than failing.
        var fallback = new CannedChatClient("from the mini");
        var client = Client(("qwen3:30b-a3b", new FailsMidStreamChatClient(Endpoint.ModelNotFound("qwen3:30b-a3b"))), ("qwen3:8b", fallback));

        await Should.ThrowAsync<ClientResultException>(() => Stream(client));
        fallback.Calls.ShouldBe(0);
    }

    [Fact]
    public void GetService_AnswersForTheBestModel()
    {
        // What the best model's client supports — structured output, above all — decides how a
        // caller shapes its request.
        var metadata = new ChatClientMetadata("ollama");

        Client(("qwen3:30b-a3b", new CannedChatClient("", metadata)), ("qwen3:8b", new CannedChatClient("")))
            .GetService<ChatClientMetadata>().ShouldBe(metadata);
    }

    private static async Task<List<string>> Stream(IChatClient client)
    {
        var words = new List<string>();
        await foreach (var update in client.GetStreamingResponseAsync(Question, cancellationToken: TestContext.Current.CancellationToken))
        {
            words.Add(update.Text);
        }

        return words;
    }
}
