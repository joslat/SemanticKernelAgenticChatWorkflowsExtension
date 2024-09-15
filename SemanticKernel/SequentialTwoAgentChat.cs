using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

public class SequentialTwoAgentChat : BaseAgentGroupChat, IAgentGroupChat
{
    private readonly List<TwoAgentChatConfiguration> _chatConfigurations;
    private ChatMessageContent? _lastWorkerMessage;

    public SequentialTwoAgentChat(List<TwoAgentChatConfiguration> chatConfigurations)
        : base(chatConfigurations.Select(c => c.WorkerAgent).Concat(chatConfigurations.Select(c => c.CriticAgent)).ToArray())
    {
        _chatConfigurations = chatConfigurations ?? throw new ArgumentNullException(nameof(chatConfigurations));
    }

    public override async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _lastWorkerMessage = this.History.LastOrDefault();

        foreach (var config in _chatConfigurations)
        {
            bool shouldTerminate = false;  // Flag to signal termination
            var workerAgent = config.WorkerAgent;
            var criticAgent = config.CriticAgent;
            var maxIterations = config.MaxIterations;
            var terminationKeyword = config.TerminationKeyword?.ToLower();
            var carryover = config.Carryover;
            var resultProcessing = config.ResultProcessing;

            // Initialize the message for worker-agent to process, respecting carryover flag
            var workerMessage = _lastWorkerMessage;

            for (int iteration = 0; iteration < maxIterations || maxIterations == 0; iteration++)
            {
                if (shouldTerminate)
                {
                    shouldTerminate = false;
                    break;  // Exit the inner loop if termination has been triggered
                }

                await foreach (var message in _agentGroupChat.InvokeAsync(workerAgent, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return message;

                    // Handle ResultProcessing based on Append or Replace
                    if (resultProcessing == ResultProcessing.Append)
                    {
                        // Append previous worker's content to the new message's content
                        workerMessage = new ChatMessageContent
                        {
                            AuthorName = message.AuthorName,
                            Content = _lastWorkerMessage.Content + "\n" + message?.Content
                        };
                    }
                    else if (resultProcessing == ResultProcessing.Replace)
                    {
                        // Replace the initial message with the last worker message
                        workerMessage = message;
                    }
                }

                // CriticAgent responds to the WorkerAgent's message
                await foreach (var criticMessage in _agentGroupChat.InvokeAsync(criticAgent, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return criticMessage;

                    if (CheckTermination(criticMessage, terminationKeyword))
                    {
                        shouldTerminate = true;
                        break;
                    }
                }
            }

            // Store the last worker message if carryover is enabled for the next agent sequence
            if (carryover)
            {
                _lastWorkerMessage = workerMessage;
            }
        }

        //output the final review
        yield return _lastWorkerMessage;

        IsComplete = true;
        yield break;
    }

    private bool CheckTermination(ChatMessageContent message, string? terminationKeyword)
    {
        if (!string.IsNullOrEmpty(terminationKeyword) && message.Content.ToLower().Contains(terminationKeyword))
        {
            return true;
        }

        return false;
    }

    public override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default)
    {
        // Streaming is not supported for this implementation
        throw new NotImplementedException();
    }
}
