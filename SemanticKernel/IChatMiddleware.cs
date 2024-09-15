using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

public interface IChatMiddleware
{
    /// <summary>
    /// the name of the middleware
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Processes a message and optionally invokes the next middleware or the target agent.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="next">Delegate to invoke the next middleware or agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed message.</returns>
    Task<ChatMessageContent> InvokeAsync(
        ChatMessageContent message,
        Func<ChatMessageContent, Task<ChatMessageContent>> next,
        CancellationToken cancellationToken = default);
}
