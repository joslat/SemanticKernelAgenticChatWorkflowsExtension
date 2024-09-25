using AgenticChatWorkflows.Helpers;
using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace AgenticChatWorkflows.AutoGen.Agents;

public static class AutoGen_MetaReviewerAgent
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

        var AutoGen_MetaReviewer = new OpenAIChatAgent(
           chatClient: client.GetChatClient(model),
           name: "MetaReviewer",
           systemMessage: $"""
                You are a meta reviewer, you aggregate and review the work of other reviewers and give a final suggestion on the content."
                
                """)
           .RegisterMessageConnector()
           .RegisterPrintMessage();

        return AutoGen_MetaReviewer;
    }
}
