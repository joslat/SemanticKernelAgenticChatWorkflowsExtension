using AgenticChatWorkflows.Helpers;
using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace AgenticChatWorkflows.AutoGen.Agents;

public static class AutoGen_LegalReviewerAgent
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

        var AutoGen_LegalReviewerAgent = new OpenAIChatAgent(
           chatClient: client.GetChatClient(model),
           name: "LegalReviewer",
           systemMessage: $"""
                You are a legal reviewer, known for your ability to ensure that content is legally compliant and free from any potential legal issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete and to the point.
                Begin the review by stating your role.
                Also be aware of data privacy and GDPR compliance which needs to be respected, so in doubt suggest the removal of PII information.
                Asume that the speakers have agreed to share their name and title, so there is no issue with sharing that in the article.
                """)
           .RegisterMessageConnector()
           .RegisterPrintMessage();

        return AutoGen_LegalReviewerAgent;
    }
}
