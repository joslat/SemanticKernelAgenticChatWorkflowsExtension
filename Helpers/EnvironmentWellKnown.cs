using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticChatWorkflows.Helpers;

public static class EnvironmentWellKnown
{
    private static string? _deploymentName;
    public static string DeploymentName => _deploymentName ??= Environment.GetEnvironmentVariable("AzureOpenAI_Model");

    private static string? _endpoint;
    public static string Endpoint => _endpoint ??= Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint");

    private static string? _apiKey;
    public static string ApiKey => _apiKey ??= Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey");

    private static string? _bingApiKey;
    public static string BingApiKey => _bingApiKey ??= Environment.GetEnvironmentVariable("Bing_ApiKey");
}