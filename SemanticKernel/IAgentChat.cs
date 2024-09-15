using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110
public interface IAgentChat
{
    bool IsActive { get; }

    Task ResetAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatMessageContent> GetChatMessagesAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatMessageContent> GetChatMessagesAsync(Agent agent, CancellationToken cancellationToken = default);

    void AddChatMessage(ChatMessageContent message);
    void AddChatMessages(IReadOnlyList<ChatMessageContent> messages);

    IAsyncEnumerable<ChatMessageContent> InvokeAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default);
}
