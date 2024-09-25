using AutoGen.Core;
using Azure.AI.OpenAI;
using Azure;

namespace AgenticChatWorkflows.Helpers;

public static class WellKnown
{
    public static void GetAutoGenModelAndConnectionToLLM(
        out string model,
        out AzureOpenAIClient client)
    {
        var key = EnvironmentWellKnown.ApiKey;
        var endpoint = EnvironmentWellKnown.Endpoint;
        model = EnvironmentWellKnown.DeploymentName; // "gpt-4o-mini";

        client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
    }
}
