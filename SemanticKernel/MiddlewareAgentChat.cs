using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

/// <summary>
/// Inspired by the AutoGen.net Middleware implementation
/// Represents a middleware-based agent chat that processes messages through a pipeline of middleware components before starting the chat.
/// those are processed before the agents in a group chat.
/// Stopped the implementation due to the complexity of the middleware implementation and having it fit Semantic Kernel.
/// </summary>
public class MiddlewareAgentChat : BaseAgentGroupChat, IAgentGroupChat
{
    private readonly List<IChatMiddleware> _middlewares = new();

    public MiddlewareAgentChat()
        : base() // Pass an empty array to the base constructor
    {
        // No agents are passed; all agent interactions are handled via middleware
    }

    // Method to add middleware
    public void AddMiddleware(IChatMiddleware middleware)
    {
        if (middleware == null) throw new ArgumentNullException(nameof(middleware));
        _middlewares.Add(middleware);
    }

    public override async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (IsComplete)
        {
            yield break;
        }

        // Retrieve the latest message from chat history as the initial message
        var initialMessages = await GetChatMessagesAsync(cancellationToken).ToListAsync(cancellationToken);
        var initialMessage = initialMessages.LastOrDefault();

        // If there's no initial message, you might want to define a default user prompt or handle accordingly
        // For this example, we'll assume that an initial message exists
        if (initialMessage == null)
        {
            throw new InvalidOperationException("No initial message found in chat history.");
        }

        // Build the middleware pipeline
        Func<ChatMessageContent, Task<ChatMessageContent>> handler = async (message) =>
        {
            // If no middleware is present, simply return the message
            return message;
        };

        // Wrap each middleware around the handler, starting from the last added middleware
        foreach (var middleware in _middlewares.AsEnumerable().Reverse())
        {
            var next = handler;
            handler = async (msg) => await middleware.InvokeAsync(msg, next, cancellationToken);
        }

        // Execute the middleware pipeline with the initial message
        var result = await handler(initialMessage);

        // Add the result to the chat history
        AddChatMessage(result);

        // Yield the result
        yield return result;

        // Check for termination based on the termination strategy
        if (CheckTermination(result))
        {
            IsComplete = true;
            yield break;
        }
    }
    private bool CheckTermination(ChatMessageContent message)
    {
        if (ExecutionSettings.TerminationStrategy != null)
        {
            // Evaluate the termination strategy with the latest message
            return ExecutionSettings.TerminationStrategy.ShouldTerminateAsync(null, new List<ChatMessageContent> { message }, CancellationToken.None).Result;
        }
        return false;
    }

    public override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming is not implemented in MiddlewareAgentChat.");
    }
}