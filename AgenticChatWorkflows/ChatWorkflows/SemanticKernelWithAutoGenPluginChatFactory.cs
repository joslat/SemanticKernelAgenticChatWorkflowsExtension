using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;
using AgenticChatWorkflows.AutoGen;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class SemanticKernelWithAutoGenPluginChatFactory
{
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
        KernelPlugin updateArticlePlugin = KernelPluginFactory.CreateFromType<UpdateArticle>();
        kernel.Plugins.Add(updateArticlePlugin);

        // Add the AutoGen plugin 
        KernelPlugin askForFeedbackAutoGen = KernelPluginFactory.CreateFromType<askForFeedbackAutoGenPlugin>();
        kernel.Plugins.Add(askForFeedbackAutoGen);

        return kernel;
    }

    public static IAgentGroupChat CreateChat(int characterLimit = 2000, int maxIterations = 1)
    {
        string projectDetails = Context.Facts;

        // Main coding agent with combined responsibilities
        ChatCompletionAgent ArticleWriterAgent = new()
        {
            Instructions = $"""
                You are a writer. You write engaging and concise articles (with title) on given topics.
                You must polish your writing based on the feedback you receive and provide a refined version.
                Only return your final work without additional comments.
                Also you will always follow the same process when writing articles:
                1. Research using the bing plugin search engine on the topic (or topics) of the article.
                2. Write the article based on the research and the input.
                3. Ask for feedback on the article by using the AskForFeedback function in the askForFeedbackAutoGenPlugin plugin, providing the article.
                4. Update the article based on the feedback.
                5. Update the article article by using UpdateArticle plugin and ask the user for feedback.
                6. Update the article based on the user's feedback.
                7. If the user is satisfied, you are done. If not, go back to step 3 unless the user asks for more research - then go back to step 1.

                """,

            Name = "CodeCrafterAgent",
            Kernel = Kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                }),
        };

        IAgentGroupChat chat = new AgentGroupChatExt(ArticleWriterAgent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new ApprovalTerminationStrategy()
                {
                    Agents = [ArticleWriterAgent],
                    MaximumIterations = maxIterations,
                }
            }
        };

        return chat;
    }

    private sealed class UpdateArticle
    {
        [KernelFunction, Description("Updates the article with the provided article.")]
        public void UpdateTheArticle(
            [Description("The article")] string article)
        {
            Context.Code = article;
        }
    }
}
