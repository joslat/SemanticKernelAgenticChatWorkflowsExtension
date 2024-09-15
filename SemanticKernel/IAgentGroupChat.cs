using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

//namespace NovelCrafter.SemanticKernel;
namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

public interface IAgentGroupChat : IAgentChat
{
    bool IsComplete { get; set; }
    ChatHistory History { get; }

    AgentGroupChatSettings ExecutionSettings { get; set; }
    IReadOnlyList<Agent> Agents { get; }

    void AddAgent(Agent agent);

    IAsyncEnumerable<ChatMessageContent> InvokeAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatMessageContent> InvokeAsync(Agent agent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(Agent agent, CancellationToken cancellationToken = default);
}

