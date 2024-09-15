using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;


#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class TwoAgentChatWorkflowFactory
{
    private const string ReviewerName = "ArtDirector";
    private const string CopyWriterName = "CopyWriter";
    private const string TerminationKeyword = "approved";

    // Lazy Kernel initialization
    private static Kernel? _kernel;
    public static Kernel Kernel => _kernel ??= CreateKernel();

    // Create the Kernel lazily using the environment variables
    private static Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IFunctionInvocationFilter, SearchFunctionFilter>();

        Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: EnvironmentWellKnown.DeploymentName,
                            endpoint: EnvironmentWellKnown.Endpoint,
                            apiKey: EnvironmentWellKnown.ApiKey)
                        .Build();

        BingConnector bing = new BingConnector(EnvironmentWellKnown.BingApiKey);
        kernel.ImportPluginFromObject(new WebSearchEnginePlugin(bing), "bing");

        return kernel;
    }

    public static IAgentGroupChat CreateChat(int characterLimit = 2000, int maxIterations = 4)
    {
        // Create agents using separate methods
        ChatCompletionAgent agentReviewer = CreateReviewerAgent();
        ChatCompletionAgent agentWriter = CreateCopyWriterAgent();

        // Create an instance of TwoAgentChat
        TwoAgentChat twoAgentChat = new TwoAgentChat(
            agentWriter, 
            agentReviewer, 
            maxIterations, 
            TerminationKeyword);

        twoAgentChat.IsComplete = false;

        return twoAgentChat;
    }

    // Method to create the Reviewer Agent
    private static ChatCompletionAgent CreateReviewerAgent()
    {
        const string reviewerInstructions = $"""
            You are an art director who has opinions about copywriting born of a love for David Ogilvy.
            The goal is to determine if the given copy is acceptable to print.
            If so, state that it is approved. Say "{TerminationKeyword}" to approve the copy.
            If not, provide insight on how to refine suggested copy without examples.
            """;

        return new ChatCompletionAgent
        {
            Instructions = reviewerInstructions,
            Name = ReviewerName,
            Kernel = Kernel,
        };
    }

    // Method to create the CopyWriter Agent
    private static ChatCompletionAgent CreateCopyWriterAgent()
    {
        const string copyWriterInstructions = """
            You are a copywriter with ten years of experience and are known for brevity and a dry humor.
            The goal is to refine and decide on the single best copy as an expert in the field.
            Only provide a single proposal per response.
            You're laser focused on the goal at hand.
            Don't waste time with chit chat.
            Consider suggestions when refining an idea.
            """;

        return new ChatCompletionAgent
        {
            Instructions = copyWriterInstructions,
            Name = CopyWriterName,
            Kernel = Kernel,
        };
    }

}