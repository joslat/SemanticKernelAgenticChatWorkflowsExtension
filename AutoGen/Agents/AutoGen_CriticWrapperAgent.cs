using AgenticChatWorkflows.Helpers;
using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace AgenticChatWorkflows.AutoGen.Agents;

public static class AutoGen_CriticWrapperAgent
{
    public static MiddlewareStreamingAgent<OpenAIChatAgent> CreateAgent(string article, string modelOverride = null)
    {
        string model;
        AzureOpenAIClient client;
        WellKnown.GetAutoGenModelAndConnectionToLLM(out model, out client);

        if (!string.IsNullOrEmpty(modelOverride))
        {
            model = modelOverride;
        }

        var CriticWrapperAgent = new OpenAIChatAgent(
           chatClient: client.GetChatClient(model),
           name: "Critic_Wrapper",
           systemMessage: $"""
                You are a meta reviewer, you aggregate and review the work of other reviewers and give a final suggestion on the content.

                You are a critic wrapper. You delegate the review work of the article to a set of specialized committee of expert reviewers and provide their feedback "as is" to help improve the quality of the content.
                you will not change at all the feedback provided by the reviewers, it will come to you aggregated and formatted.
                you will not change the format. You will not change the feedback. You will not change the content.          
                you will hand over the article "as is" to the expert reviewers.
                The Article is: 
                ---
                {article}
                ---
                """)
           .RegisterMessageConnector()
           .RegisterPrintMessage();

        return CriticWrapperAgent;
    }
}
