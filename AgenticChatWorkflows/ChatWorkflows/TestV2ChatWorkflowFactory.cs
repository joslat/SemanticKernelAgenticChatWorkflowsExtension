using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;


#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class TestV2ChatWorkflowFactory
{

    private const string ReviewerName = "ArtDirector";
    private const string ReviewerInstructions =
        """
        You are an art director who has opinions about copywriting born of a love for David Ogilvy.
        The goal is to determine if the given copy is acceptable to print.
        If so, state that it is approved.
        If not, provide insight on how to refine suggested copy without examples.
        """;

    private const string CopyWriterName = "CopyWriter";
    private const string CopyWriterInstructions =
        """
        You are a copywriter with ten years of experience and are known for brevity and a dry humor.
        The goal is to refine and decide on the single best copy as an expert in the field.
        Only provide a single proposal per response.
        You're laser focused on the goal at hand.
        Don't waste time with chit chat.
        Consider suggestions when refining an idea.
        """;

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
        // Define the agents
        ChatCompletionAgent agentReviewer =
            new()
            {
                Instructions = ReviewerInstructions,
                Name = ReviewerName,
                Kernel = Kernel,
            };

        ChatCompletionAgent agentWriter =
            new()
            {
                Instructions = CopyWriterInstructions,
                Name = CopyWriterName,
                Kernel = Kernel,
            };

        KernelFunction terminationFunction =
            KernelFunctionFactory.CreateFromPrompt(
                """
                Determine if the copy has been approved.  If so, respond with a single word: yes

                History:
                {{$history}}
                """);

        KernelFunction selectionFunction =
            KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                Determine which participant takes the next turn in a conversation based on the the most recent participant.
                State only the name of the participant to take the next turn.
                No participant should take more than one turn in a row.
                
                Choose only from these participants:
                - {{{ReviewerName}}}
                - {{{CopyWriterName}}}
                
                Always follow these rules when selecting the next participant:
                - After {{{CopyWriterName}}}, it is {{{ReviewerName}}}'s turn.
                - After {{{ReviewerName}}}, it is {{{CopyWriterName}}}'s turn.

                History:
                {{$history}}
                """);

        // Create a chat for agent interaction.
        IAgentGroupChat chatExt =
            new AgentGroupChatExt(agentWriter, agentReviewer)
            {
                ExecutionSettings =
                    new()
                    {
                        // Here KernelFunctionTerminationStrategy will terminate
                        // when the art-director has given their approval.
                        TerminationStrategy =
                            new KernelFunctionTerminationStrategy(terminationFunction, Kernel)
                            {
                                // Only the art-director may approve.
                                Agents = [agentReviewer],
                                // Customer result parser to determine if the response is "yes"
                                ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                                // The prompt variable name for the history argument.
                                HistoryVariableName = "history",
                                // Limit total number of turns
                                MaximumIterations = 10,
                            },
                        // Here a KernelFunctionSelectionStrategy selects agents based on a prompt function.
                        SelectionStrategy =
                            new KernelFunctionSelectionStrategy(selectionFunction, Kernel)
                            {
                                // Always start with the writer agent.
                                InitialAgent = agentWriter,
                                // Returns the entire result value as a string.
                                ResultParser = (result) => result.GetValue<string>() ?? CopyWriterName,
                                // The prompt variable name for the agents argument.
                                AgentsVariableName = "agents",
                                // The prompt variable name for the history argument.
                                HistoryVariableName = "history",
                            },
                    }
            };

        return chatExt;
    }


}


///create a concept idea for a story regarding Generative AI in the future, the rise of the machines, AI Agents and a developer that saves the world.
/// Science fiction thriller with romance, so he saves the girl
