using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

public class AgentGroupChatExt : IAgentGroupChat
{
    private readonly AgentGroupChat _agentGroupChat;

    public AgentGroupChatExt(params Agent[] agents)
    {
        _agentGroupChat = new AgentGroupChat(agents);
    }

    public AgentGroupChatExt(AgentGroupChat agentGroupChat)
    {
        _agentGroupChat = agentGroupChat;
    }

    // Expose the ChatHistory from the underlying _agentGroupChat
    //public ChatHistory History => _agentGroupChat.History; //doesn't work as History is protected
    public ChatHistory History => _agentGroupChat.GetType().GetProperty("History", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_agentGroupChat) as ChatHistory;

    // Properties from IAgentChat
    public bool IsActive => _agentGroupChat.IsActive;

    public bool IsComplete
    {
        get => _agentGroupChat.IsComplete;
        set => _agentGroupChat.IsComplete = value;
    }

    public AgentGroupChatSettings ExecutionSettings
    {
        get => _agentGroupChat.ExecutionSettings;
        set => _agentGroupChat.ExecutionSettings = value;
    }

    public IReadOnlyList<Agent> Agents => _agentGroupChat.Agents;

    // Methods from IAgentChat
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.ResetAsync(cancellationToken);
    }

    public void AddChatMessage(ChatMessageContent message)
    {
        _agentGroupChat.AddChatMessage(message);
    }

    public void AddChatMessages(IReadOnlyList<ChatMessageContent> messages)
    {
        _agentGroupChat.AddChatMessages(messages);
    }

    public IAsyncEnumerable<ChatMessageContent> GetChatMessagesAsync(CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.GetChatMessagesAsync(cancellationToken);
    }

    public IAsyncEnumerable<ChatMessageContent> GetChatMessagesAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.GetChatMessagesAsync(agent, cancellationToken);
    }

    // Methods from IAgentGroupChat
    public void AddAgent(Agent agent)
    {
        _agentGroupChat.AddAgent(agent);
    }

    public async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _agentGroupChat.InvokeAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _agentGroupChat.InvokeStreamingAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(Agent agent, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _agentGroupChat.InvokeStreamingAsync(agent, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<ChatMessageContent> InvokeAsync(Agent agent, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _agentGroupChat.InvokeAsync(agent, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
