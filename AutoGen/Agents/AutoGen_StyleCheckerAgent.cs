using AgenticChatWorkflows.Helpers;
using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace AgenticChatWorkflows.AutoGen.Agents;

public static class AutoGen_StyleCheckerAgent
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

        var AutoGen_StyleCheckerAgent = new OpenAIChatAgent(
           chatClient: client.GetChatClient(model),
           name: "FactChecker",
           systemMessage: $"""
                You are a Style Checker. Your task is to ensure that the article is written in a proper style. 
                Check that the writing style is positive, engaging, motivational, original, and funny. 
                The phrases should not be too complex, and the tone should be friendly, casual, yet polite.
                Provide suggestions to improve the style if necessary.
                Provide ONLY Suggestions, do not rewrite the content or write any part of the content in the style suggested, this is the work of the writer.
                You are a Style Checker. Your task is to ensure that the article is written in a proper style. 
                Check that the writing style is positive, engaging, motivational, original, and funny. 
                The phrases should not be too complex, and the tone should be friendly, casual, yet polite.
                Provide suggestions to improve the style if necessary.
                Provide ONLY Suggestions, do not rewrite the content or write any part of the content in the style suggested, this is the work of the writer.
                """)
           .RegisterMessageConnector()
           .RegisterPrintMessage();

        return AutoGen_StyleCheckerAgent;
    }
}
