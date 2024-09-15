using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

public class TwoAgentChat : BaseAgentGroupChat, IAgentGroupChat
{
    private readonly ChatCompletionAgent _workerAgent;
    private readonly ChatCompletionAgent _criticAgent;
    private readonly int _maxIterations;
    private readonly string _terminationKeyword;

    public TwoAgentChat(
        ChatCompletionAgent workerAgent, 
        ChatCompletionAgent criticAgent, 
        int maxIterations, 
        string terminationKeyword)
        : base(workerAgent, criticAgent)
    {
        _workerAgent = workerAgent;
        _criticAgent = criticAgent;
        _maxIterations = maxIterations;
        _terminationKeyword = terminationKeyword.ToLower();
    }

    public override async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int iteration = 0; iteration < _maxIterations; iteration++)
        {
            // WorkerAgent produces a message
            await foreach (var workerMessage in _agentGroupChat.InvokeAsync(_workerAgent, cancellationToken).WithCancellation(cancellationToken))
            {
                yield return workerMessage;
            }

            // CriticAgent responds to the WorkerAgent's message
            await foreach (var criticMessage in _agentGroupChat.InvokeAsync(_criticAgent, cancellationToken).WithCancellation(cancellationToken))
            {
                yield return criticMessage;

                if (CheckTermination(criticMessage))
                {
                    IsComplete = true;
                    yield break;
                }
            }

            if (IsComplete)
            {
                break;
            }
        }
    }

    public override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default)
    {
        // Not implemented 
        throw new NotImplementedException();
    }

    private bool CheckTermination(ChatMessageContent message)
    {
        if (message.Content.ToLower().Contains(_terminationKeyword))
        {
            IsComplete = true;

            return true;
        }

        return false;
    }

}