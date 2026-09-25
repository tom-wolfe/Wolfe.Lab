using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// One chat client over several models, best first.
/// </summary>
internal sealed class FallbackChatClient(IReadOnlyList<ChatModel> models, ILogger<FallbackChatClient> logger) : IChatClient
{
    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var conversation = messages as IReadOnlyList<ChatMessage> ?? [.. messages];
        for (var i = 0; ; i++)
        {
            try
            {
                return await models[i].Client.GetResponseAsync(conversation, options, ct);
            }
            catch (Exception ex) when (IsModelNotFound(ex) && i + 1 < models.Count)
            {
                FallingBack(i);
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Falls back only before the first update: a model that has started answering has been chosen,
    /// and switching mid-stream would splice two models' answers together.
    /// </remarks>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var conversation = messages as IReadOnlyList<ChatMessage> ?? [.. messages];
        for (var i = 0; ; i++)
        {
            await using var updates = models[i].Client.GetStreamingResponseAsync(conversation, options, ct)
                .GetAsyncEnumerator(ct);
            bool any;
            try
            {
                any = await updates.MoveNextAsync();
            }
            catch (Exception ex) when (IsModelNotFound(ex) && i + 1 < models.Count)
            {
                FallingBack(i);
                continue;
            }

            if (!any)
            {
                yield break;
            }

            yield return updates.Current;
            while (await updates.MoveNextAsync())
            {
                yield return updates.Current;
            }

            yield break;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Answered by the best model's client: what it supports — structured output, above all —
    /// decides how a caller shapes its request.
    /// </remarks>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : models[0].Client.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var model in models)
        {
            model.Client.Dispose();
        }
    }

    /// <summary>
    /// Whether the endpoint said it does not have the model — ollama's 404 — as opposed to not
    /// answering at all.
    /// </summary>
    private static bool IsModelNotFound(Exception ex) => ex is ClientResultException { Status: 404 };

    private void FallingBack(int from) =>
        logger.LogInformation("{Model} is not being served; asking {Next}.", models[from].Name, models[from + 1].Name);
}
