using Microsoft.Extensions.AI;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// One model, and the client that asks it.
/// </summary>
/// <param name="Name">The model's name as the endpoint knows it.</param>
/// <param name="Client">A client bound to that model.</param>
internal sealed record ChatModel(string Name, IChatClient Client);
