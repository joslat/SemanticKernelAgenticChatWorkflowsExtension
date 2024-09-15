using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class SequentialAgentChatWorkflowFactory
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

    //public static AgentGroupChat CreateChat() => (AgentGroupChat)CreateChat(2000);

    public static IAgentGroupChat CreateChat(int characterLimit = 2000)
    {
        // Create agents using separate methods
        var seoReviewerAgent = CreateSEOReviewerAgent();
        var legalReviewerAgent = CreateLegalReviewerAgent();
        var ethicsReviewerAgent = CreateEthicsReviewerAgent();
        var factCheckerAgent = CreateFactCheckerAgent();
        var styleCheckerAgent = CreateStyleCheckerAgent();
        var metaReviewerAgent = CreateMetaReviewerAgent();

        // List of agents to invoke sequentially
        var agents = new List<ChatCompletionAgent>
            {
                seoReviewerAgent,
                legalReviewerAgent,
                ethicsReviewerAgent,
                factCheckerAgent,
                styleCheckerAgent
            };

        // Create the SequentialAgentChat with a list of agents and a summarizer agent
        SequentialAgentChat sequentialAgentChat = new SequentialAgentChat(
            agents,
            metaReviewerAgent);

        sequentialAgentChat.IsComplete = false;

        return sequentialAgentChat;
    }

    #region Agent Creation Methods
    private static ChatCompletionAgent CreateSEOReviewerAgent()
    {
        const string seoReviewerInstructions = @"
                You are an SEO reviewer, known for your ability to optimize content for search engines, ensuring that it ranks well and attracts organic traffic.
                Make sure your suggestion is concise (within 3 bullet points), concrete, and to the point.
                Begin the review by stating your role.
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
                You are a legal reviewer, known for your ability to ensure that content is legally compliant and free from any potential legal issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete, and to the point.
                Begin the review by stating your role.
                Also be aware of data privacy and GDPR compliance, which needs to be respected, so in doubt suggest the removal of PII information.
                Assume that the speakers have agreed to share their name and title, so there is no issue with sharing that in the article.
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
                You are an ethics reviewer, known for your ability to ensure that content is ethically sound and free from any potential ethical issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete, and to the point.
                Begin the review by stating your role.
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
        string conferenceDescription = GetConferenceDescription();

        string factCheckerInstructions = $@"
                You are a FactChecker. Your job is to ensure that all facts mentioned in the article are accurate and derived from the provided source material.
                You will avoid any hallucinations and ensure that all the facts are cross-checked with the source material.

                Cross-check the content against the following source:

                {conferenceDescription}

                Make sure no invented information is included, and suggest corrections if any discrepancies are found.
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
                You are a Style Checker. Your task is to ensure that the article is written in a proper style.
                Check that the writing style is positive, engaging, motivational, original, and funny.
                The phrases should not be too complex, and the tone should be friendly, casual, yet polite.
                Provide suggestions to improve the style if necessary.
                Provide ONLY suggestions, do not rewrite the content or write any part of the content in the style suggested; this is the work of the writer.
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
                You are a meta reviewer. You aggregate and review the work of other reviewers and give a summarized final review from each reviewer on the content. Do not output the content, just provide a summary of the feedback from each reviewer. 
                Ensure that all feedback is constructive and actionable.
                ";

        return new ChatCompletionAgent
        {
            Instructions = metaReviewerInstructions,
            Name = "Meta_Reviewer",
            Kernel = Kernel,
        };
    }

    private static string GetConferenceDescription()
    {
        return @"
                The .NET Day Switzerland is a community-driven and independent .NET conference focused on .NET technologies, taking place on June 3rd, 2024, in Zürich, Switzerland.
                The conference features 3 parallel tracks with 15 sessions, covering topics like .NET, Azure, Blazor, WebAssembly, AI, and more.
                Speakers include renowned experts from the industry.
                The event is non-profit, organized by the .NET community, and aims to bring developers, architects, and experts together.
                ";
    }

    #endregion

}