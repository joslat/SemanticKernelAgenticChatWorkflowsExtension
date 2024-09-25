using AgenticChatWorkflows.Helpers;
using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace AgenticChatWorkflows.AutoGen.Agents;

public static class AutoGen_EthicsReviewerAgent
{
    public static MiddlewareStreamingAgent<OpenAIChatAgent> CreateAgent(string modelOverride = null)
    {
        string model;
        AzureOpenAIClient client;
        WellKnown.GetAutoGenModelAndConnectionToLLM(out model, out client);

        if (!string.IsNullOrEmpty(modelOverride))
        {
            model = modelOverride;
        }

        var AutoGen_EthicsReviewerAgent = new OpenAIChatAgent(
           chatClient: client.GetChatClient(model),
           name: "EthicsReviewer",
           systemMessage: $"""
                You are an ethics reviewer, known for your ability to ensure that content is ethically sound and free from any potential ethical issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete and to the point.
                Begin the review by stating your role.
                """)
           .RegisterMessageConnector()
           .RegisterPrintMessage();

        return AutoGen_EthicsReviewerAgent;
    }
}
