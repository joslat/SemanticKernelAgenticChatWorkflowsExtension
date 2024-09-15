using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Metadata;
using System;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

// Doesn't seem to fully work - reimplement without the AggregatorAgent seems the solution... something is amiss here...
public static class NestedChatWithGroupAgentChatWorkflowFactory
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
    
    public static IAgentGroupChat CreateChat(int maxIterations = 5)
    {
        // Create worker agent
        var workerAgent = CreateWorkerAgent();

        // Create all reviewer agents for the sequential process
        var seoReviewerAgent = CreateSEOReviewerAgent();
        var legalReviewerAgent = CreateLegalReviewerAgent();
        var ethicsReviewerAgent = CreateEthicsReviewerAgent();
        var factCheckerAgent = CreateFactCheckerAgent();
        var styleCheckerAgent = CreateStyleCheckerAgent();
        var metaReviewerAgent = CreateMetaReviewerAgent();

        // Sequential agents list for the SequentialAgentChat
        var sequentialAgents = new List<ChatCompletionAgent>
            {
                seoReviewerAgent,
                legalReviewerAgent,
                ethicsReviewerAgent,
                factCheckerAgent,
                styleCheckerAgent
            };
        string OuterTerminationInstructions =
            $$$"""
            Determine if user request has been fully answered.
            Respond only with "APPROVED" if the user request has been fully answered
            """;

        KernelFunction outerTerminationFunction = KernelFunctionFactory.CreateFromPrompt(OuterTerminationInstructions);

        // Wrap SequentialAgentChat using AggregatorAgent with Nested Mode
        AggregatorAgent sequentialChatAgent =
            new(CreateSequentialAgentChat)
            {
                Name = "SequentialChatAgent",
                Mode = AggregatorMode.Nested,
            };

        AgentGroupChat CreateSequentialAgentChat() =>
            new(seoReviewerAgent,
                legalReviewerAgent,
                ethicsReviewerAgent,
                factCheckerAgent,
                styleCheckerAgent)
            {
                ExecutionSettings =
                    new()
                    {
                        TerminationStrategy =
                            new KernelFunctionTerminationStrategy(outerTerminationFunction, Kernel)
                            {
                                ResultParser =
                                    (result) =>
                                    {
                                        var outcome = result.GetValue<string>().ToLower();

                                        return (outcome == "approved");
                                    },
                                MaximumIterations = 1,
                            },
                    }
            };

        // Create the final critic agent
        var finalCriticAgent = CreateFinalCriticAgent();

        // Create the AgentGroupChat combining the worker, sequential reviewers, and final critic
        AgentGroupChat groupChat = new(workerAgent, sequentialChatAgent, finalCriticAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                TerminationStrategy = CreateTerminationStrategy(3)
            }
        };

        //return groupChat; does not work as AgentGroupChat is not IAgentGroupChat (does not implememt any interface but inherits from AgentChat - not coding vs interfaces...) I implemented IAGentGroupChat locally to create custom versions of AgentGroupChat :)

        // Solution: Create a custom version of AgentGroupChat that implements IAgentGroupChat and wraps it - AgentGroupChatExt
        AgentGroupChatExt agentGroupChatExt = new(groupChat);
        return agentGroupChatExt;
    }

    #region Agent Creation Methods

    private static ChatCompletionAgent CreateWorkerAgent()
    {
        const string workerInstructions = @"
                You are a skilled article writer. Write a detailed and engaging article on a given topic.
                Keep the language clear and concise. Only return the article without additional commentary.
            ";

        return new ChatCompletionAgent
        {
            Instructions = workerInstructions,
            Name = "WorkerAgent",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateSEOReviewerAgent()
    {
        const string seoReviewerInstructions = @"
                You are an SEO reviewer. Ensure the article is optimized for search engines, concise, and engaging.
                Provide suggestions in a concise manner (within 3 bullet points).
            ";

        return new ChatCompletionAgent
        {
            Instructions = seoReviewerInstructions,
            Name = "SEO_Reviewer",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateLegalReviewerAgent()
    {
        const string legalReviewerInstructions = @"
                You are a legal reviewer. Ensure the article complies with legal standards and is free from potential legal issues.
                Make suggestions concisely and focus on any legal problems.
            ";

        return new ChatCompletionAgent
        {
            Instructions = legalReviewerInstructions,
            Name = "Legal_Reviewer",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateEthicsReviewerAgent()
    {
        const string ethicsReviewerInstructions = @"
                You are an ethics reviewer. Ensure the article adheres to ethical standards and avoids any controversial topics.
                Suggest ethical improvements concisely.
            ";

        return new ChatCompletionAgent
        {
            Instructions = ethicsReviewerInstructions,
            Name = "Ethics_Reviewer",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateFactCheckerAgent()
    {
        const string factCheckerInstructions = @"
                You are a fact-checker. Ensure that all factual claims in the article are accurate and backed by reliable sources.
                Suggest corrections if there are any factual inaccuracies.
            ";

        return new ChatCompletionAgent
        {
            Instructions = factCheckerInstructions,
            Name = "FactChecker",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateStyleCheckerAgent()
    {
        const string styleCheckerInstructions = @"
                You are a style checker. Ensure the writing style is engaging, clear, and suitable for the target audience.
                Provide feedback on style issues concisely.
            ";

        return new ChatCompletionAgent
        {
            Instructions = styleCheckerInstructions,
            Name = "StyleChecker",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateMetaReviewerAgent()
    {
        const string metaReviewerInstructions = @"
                You are a meta reviewer. You aggregate and review the work of other reviewers and give a summarized final review from each reviewer on the content.
                Ensure that all feedback is constructive and actionable.
            ";

        return new ChatCompletionAgent
        {
            Instructions = metaReviewerInstructions,
            Name = "Meta_Reviewer",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateFinalCriticAgent()
    {
        const string finalCriticInstructions = @"
                You are the final critic. Review the article holistically and approve it for publication if it meets all standards.
                Provide any final feedback or approve it for publishing.
            ";

        return new ChatCompletionAgent
        {
            Instructions = finalCriticInstructions,
            Name = "FinalCritic",
            Kernel = Kernel,
        };
    }

    #endregion

    #region Strategy Creation Methods

    private static TerminationStrategy CreateTerminationStrategy(int maxIterations)
    {
        return new AgentTerminationStrategy
        {
            MaximumIterations = maxIterations
        };
    }


    #endregion



}


public sealed class AgentTerminationStrategy : TerminationStrategy
{
    /// <inheritdoc/>
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}


// write me an article about .NET Day Switzerland
//.NET Day Switzerland takes place on Tuesday, the 27.08.2024 at the Arena Cinemas at Sihlcity in Zürich and is an independent technology conference for developers, architects and experts to discuss about and get to know.NET technologies all around.NET, .NET Core, C#, ASP.NET Core, Azure and more. Experienced speakers share their knowledge on the latest topics and give you deep insights into the new world of Microsoft software development and beyond. In addition to the technical talks, the .NET Day provides a space for discussions with the speakers and other attendees.

//The .NET Day is your place for networking, discussions and questions!

//.NET Day Switzerland is a non-profit community conference.All the speakers and staff engage on a voluntary basis because they are good people and want to support the Swiss.NET Community. Any positive financial balance from the ticket sales will be used to support non-profit organizations either involved in charity projects or the Swiss software developer community.

//Questions, input or improvements can be dropped at any time via info[at] dotnetday.ch

//If you want to get the hottest news delivered to your inbox, sign up for the.NET Day Newsletter here. Your email address will not be abused or given to anybody outside. That’s our promise!

//If you are interested in the history and background of.NET Day Switzerland read this blog post: here

