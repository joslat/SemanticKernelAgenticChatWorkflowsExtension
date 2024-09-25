using AgenticChatWorkflows.Helpers;
using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace AgenticChatWorkflows.AutoGen.Agents;

public static class AutoGen_SEOReviewerAgent
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

        var SEOReviewerAgent = new OpenAIChatAgent(
           chatClient: client.GetChatClient(model),
           name: "SEOReviewer",
           systemMessage: $"""
                You are an SEO reviewer, known for your ability to optimize content for search engines, ensuring that it ranks well and attracts organic traffic.
                Make sure your suggestion is concise (within 3 bullet points), concrete and to the point.
                Begin the review by stating your role.
                """)
           .RegisterMessageConnector()
           .RegisterPrintMessage();

        return SEOReviewerAgent;
    }
}
