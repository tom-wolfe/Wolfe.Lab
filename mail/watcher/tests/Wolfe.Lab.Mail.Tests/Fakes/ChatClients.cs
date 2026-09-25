using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Wolfe.Lab.Mail.Tests.Fakes;

/// <summary>
/// Answers every request with the same text, whole or as a stream of words; reports
/// <paramref name="service"/> to anyone who asks for its type.
/// </summary>
internal sealed class CannedChatClient(string answer, object? service = null) : IChatClient
{
    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls++;
        foreach (var word in answer.Split(' '))
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, word);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(service) ? service : null;

    public void Dispose()
    {
    }
}

/// <summary>Fails every request the same way — before its first update, when streamed.</summary>
internal sealed class BrokenChatClient(Exception failure) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw failure;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        throw failure;
#pragma warning disable CS0162 // An iterator needs a yield to be one.
        yield break;
#pragma warning restore CS0162
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>The failures an OpenAI-compatible endpoint answers with.</summary>
internal static class Endpoint
{
    /// <summary>An endpoint that cannot be reached at all.</summary>
    public static HttpRequestException Down() => new("connection refused");
}
