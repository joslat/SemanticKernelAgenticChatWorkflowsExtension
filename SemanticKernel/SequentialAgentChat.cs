using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

/// <summary>
/// It implements a sequence of agents that are executed in order. 
/// At the end the outputs from all agents are concatenated and passed to a summarizer agent.
/// </summary>
public class SequentialAgentChat : BaseAgentGroupChat, IAgentGroupChat
{
    private readonly List<ChatCompletionAgent> _agents;
    private readonly ChatCompletionAgent _summarizerAgent;
    private readonly int _maxIterations = 1;
    private readonly string? _terminationKeyword;

    public SequentialAgentChat(
        List<ChatCompletionAgent> agents,
        ChatCompletionAgent summarizerAgent)
        : base(agents.ToArray())
    {
        if (agents == null)
        {
            throw new ArgumentException("There must be at least one agent.");
        }

        _agents = agents;
        _summarizerAgent = summarizerAgent;
        _maxIterations = 1; // only one iteration is supported
    }

    public override async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = new List<ChatMessageContent>();
        for (int iteration = 0; iteration < _maxIterations || _maxIterations == 0; iteration++)
        {
            foreach (var agent in _agents)
            {
                // Invoke each agent in sequence
                await foreach (var agentMessage in _agentGroupChat.InvokeAsync(agent, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return agentMessage;
                    history.Add(agentMessage);
                }
            }
        }

        // Summarizer Agent
        if (_summarizerAgent != null)
        {
            var concatenatedMessages = string.Join("\n", history.Select(msg => msg.Content));

            var summaryMessage = new ChatMessageContent
            {
                Content = concatenatedMessages
            };

            // Pass the concatenated messages to the summarizer
            await foreach (var summaryResponse in _agentGroupChat.InvokeAsync(_summarizerAgent, cancellationToken).WithCancellation(cancellationToken))
            {
                yield return summaryResponse;
            }

            IsComplete = true;
            yield break;
        }
    }

    public override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
