using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using System.Net;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Reflection.Metadata;
using System.Windows.Media;


#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class TestWorkflowChatFactory
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

        return kernel;
    }

    public static IAgentGroupChat CreateChat(int characterLimit = 2000, int maxIterations = 25)
    {
        string facts = Context.Facts;

        ChatCompletionAgent questionAnswererAgent = new()
        {
            Instructions = $"""
                You are a question answerer for {facts}.
                You take in questions from a questionnaire and emit the answers from the perspective of {facts},
                using documentation from the public web. You also emit links to any websites you find that help answer the questions.
                Do not address the user as 'you' - make all responses solely in the third person.
                If you do not find information on a topic, you simply respond that there is no information available on that topic.
                You will emit an answer that is no greater than {characterLimit} characters in length.
                """,
            Name = "QuestionAnswererAgent",
            Kernel = Kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                }),
        };

        ChatCompletionAgent answerCheckerAgent = new()
        {
            Instructions = $"""
                You are an answer checker for {facts}. Your responses always start with either the words ANSWER CORRECT or ANSWER INCORRECT.
                Given a question and an answer, you check the answer for accuracy regarding {facts},
                using public web sources when necessary. If everything in the answer is true, you verify the answer by responding "ANSWER CORRECT." with no further explanation.
                You also ensure that the answer is no greater than {characterLimit} characters in length.
                Otherwise, you respond "ANSWER INCORRECT - " and add the portion that is incorrect.
                You do not output anything other than "ANSWER CORRECT" or "ANSWER INCORRECT - <portion>".
                """,
            Name = "AnswerCheckerAgent",
            Kernel = Kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                }),
        };

        ChatCompletionAgent linkCheckerAgent = new()
        {
            Instructions = """
                You are a link checker. Your responses always start with either the words LINKS CORRECT or LINK INCORRECT.
                Given a question and an answer that contains links, you verify that the links are working,
                using public web sources when necessary. If all links are working, you verify the answer by responding "LINKS CORRECT" with no further explanation.
                Otherwise, for each bad link, you respond "LINK INCORRECT - " and add the link that is incorrect.
                You do not output anything other than "LINKS CORRECT" or "LINK INCORRECT - <link>".
                """,
            Name = "LinkCheckerAgent",
            Kernel = Kernel
        };

        ChatCompletionAgent managerAgent = new()
        {
            Instructions = """
                You are a manager which reviews the question, the answer to the question, and the links.
                If the answer checker replies "ANSWER INCORRECT", or the link checker replies "LINK INCORRECT," you can reply "reject" and ask the question answerer to correct the answer.
                Once the question has been answered properly, you can approve the request by just responding "approve".
                You do not output anything other than "reject" or "approve".
                """,
            Name = "ManagerAgent",
            Kernel = Kernel
        };

        IAgentGroupChat chat = new AgentGroupChatExt(questionAnswererAgent, answerCheckerAgent, linkCheckerAgent, managerAgent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new ApprovalTerminationStrategy()
                {
                    Agents = [managerAgent],
                    MaximumIterations = maxIterations,
                }
            }
        };

        return chat;
    }





}
