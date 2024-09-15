using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0110

namespace Microsoft.SemanticKernel.Agents;

public abstract class BaseAgentGroupChat : IAgentGroupChat
{
    protected readonly IAgentGroupChat _agentGroupChat;

    protected BaseAgentGroupChat(params ChatCompletionAgent[] agents)
    {
        _agentGroupChat = new AgentGroupChatExt(agents);
    }

    // Properties from IAgentChat
    public bool IsActive => _agentGroupChat.IsActive;

    // Properties from IAgentGroupChat
    public ChatHistory History => _agentGroupChat.History;

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
    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.ResetAsync(cancellationToken);
    }

    public virtual void AddChatMessage(ChatMessageContent message)
    {
        _agentGroupChat.AddChatMessage(message);
    }

    public virtual void AddChatMessages(IReadOnlyList<ChatMessageContent> messages)
    {
        _agentGroupChat.AddChatMessages(messages);
    }

    public virtual IAsyncEnumerable<ChatMessageContent> GetChatMessagesAsync(CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.GetChatMessagesAsync(cancellationToken);
    }

    public virtual IAsyncEnumerable<ChatMessageContent> GetChatMessagesAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.GetChatMessagesAsync(agent, cancellationToken);
    }

    // Methods from IAgentGroupChat
    public virtual void AddAgent(Agent agent)
    {
        _agentGroupChat.AddAgent(agent);
    }

    // Abstract methods to be implemented by derived classes
    public abstract IAsyncEnumerable<ChatMessageContent> InvokeAsync(CancellationToken cancellationToken = default);

    public abstract IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default);

    public virtual IAsyncEnumerable<ChatMessageContent> InvokeAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.InvokeAsync(agent, cancellationToken);
    }

    public virtual IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        return _agentGroupChat.InvokeStreamingAsync(agent, cancellationToken);
    }

}