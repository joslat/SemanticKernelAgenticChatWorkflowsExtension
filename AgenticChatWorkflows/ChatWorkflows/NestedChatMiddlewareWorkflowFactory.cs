using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticChatWorkflows;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

/// <summary>
/// NestedChat Middleware Workflow Factory.
/// Stopped implementation due time constrains and not fully fitting the Semantic Kernel pattern - they are used in an agent, not a chat, figured out that later.
/// will come back to this a bit later.
/// </summary>
public static class NestedChatMiddlewareWorkflowFactory
{
    private const string WriterName = "Writer";
    private const string CriticName = "Critic";
    private const string TerminationKeyword = "TERMINATE";

    // Lazy Kernel initialization
    private static Kernel? _kernel;
    public static Kernel Kernel => _kernel ??= CreateKernel();

    // Create the Kernel lazily using the environment variables
    private static Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        // Add any required services or plugins to the kernel

        Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: EnvironmentWellKnown.DeploymentName,
                            endpoint: EnvironmentWellKnown.Endpoint,
                            apiKey: EnvironmentWellKnown.ApiKey)
                        .Build();

        // Add any additional plugins if needed

        return kernel;
    }

    public static IAgentGroupChat CreateChat(int maxIterations = 5)
    {
        // Create agents
        var writerAgent = CreateWriterAgent();
        var seoReviewerAgent = CreateSEOReviewerAgent();
        var legalReviewerAgent = CreateLegalReviewerAgent();
        var ethicsReviewerAgent = CreateEthicsReviewerAgent();
        var factCheckerAgent = CreateFactCheckerAgent();
        var styleCheckerAgent = CreateStyleCheckerAgent();
        var metaReviewerAgent = CreateMetaReviewerAgent();
        var criticAgent = CreateCriticAgent();

        // Aggregate all agents into a list
        var allAgents = new List<Agent>
            {
                writerAgent,
                seoReviewerAgent,
                legalReviewerAgent,
                ethicsReviewerAgent,
                factCheckerAgent,
                styleCheckerAgent,
                metaReviewerAgent,
                criticAgent
            };

        // Create MiddlewareAgentChat without passing any agents
        var middlewareChat = new MiddlewareAgentChat();

        // Add the NestedChatMultiCriticMiddleware, passing the writer and all agents
        middlewareChat.AddMiddleware(new NestedChatMultiCriticMiddleware());

        return middlewareChat;
    }

    private static bool CheckTermination(ChatMessageContent message)
    {
        if (message.Content.ToLower().Contains(TerminationKeyword))
        {
            return true;
        }

        return false;
    }

    #region Agent Creation Methods

    /// <summary>
    /// Creates the Writer agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for writing.</returns>
    private static ChatCompletionAgent CreateWriterAgent()
    {
        const string writerInstructions = @"
            You are a writer. You write engaging and concise articles (with title) on given topics.
            You must polish your writing based on the feedback you receive and provide a refined version.
            Only return your final work without additional comments.
            ";

        return new ChatCompletionAgent
        {
            Instructions = writerInstructions,
            Name = "Writer",
            Kernel = Kernel,
        };
    }

    /// <summary>
    /// Creates the SEO Reviewer agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for SEO review.</returns>
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

    /// <summary>
    /// Creates the Legal Reviewer agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for legal review.</returns>
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

    /// <summary>
    /// Creates the Ethics Reviewer agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for ethics review.</returns>
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

    /// <summary>
    /// Creates the FactChecker agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for fact checking.</returns>
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

    /// <summary>
    /// Creates the StyleChecker agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for style checking.</returns>
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

    /// <summary>
    /// Creates the MetaReviewer agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for meta reviewing.</returns>
    private static ChatCompletionAgent CreateMetaReviewerAgent()
    {
        const string metaReviewerInstructions = @"
            You are a meta reviewer. You aggregate and review the work of other reviewers and give final suggestions on the content.
            ";

        return new ChatCompletionAgent
        {
            Instructions = metaReviewerInstructions,
            Name = "Meta_Reviewer",
            Kernel = Kernel,
        };
    }

    /// <summary>
    /// Creates the Critic agent.
    /// </summary>
    /// <returns>Configured ChatCompletionAgent for critiquing.</returns>
    private static ChatCompletionAgent CreateCriticAgent()
    {
        string criticInstructions = $@"
            You are a critic. You review the work of the writer and provide constructive feedback to help improve the quality of the content.
            If the work is already solid and convincing, like 80-90% perfect, you can respond with '{TerminationKeyword}' only.
            If you provide ANY feedback, DO NOT, I repeat, DO NOT respond or add '{TerminationKeyword}' in your feedback.
            After having replied 4 times, respond with '{TerminationKeyword}' to end the conversation.
            AGAIN DO NOT WRITE ANY PART OF THE WORK. ONLY PROVIDE FEEDBACK.
            IF THE WORK IS SOLID, RESPOND WITH '{TerminationKeyword}'.
            RESPOND WITH {TerminationKeyword} AFTER 4 REPLIES.
            ";

        return new ChatCompletionAgent
        {
            Instructions = criticInstructions,
            Name = "Critic",
            Kernel = Kernel,
        };
    }

    /// <summary>
    /// Provides a description of the conference for fact checking.
    /// </summary>
    /// <returns>Conference description string.</returns>
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

public class NestedChatMultiCriticMiddleware : IChatMiddleware
{
    public string? Name => nameof(NestedChatMultiCriticMiddleware);

    private readonly ChatCompletionAgent _writerAgent;
    private readonly IEnumerable<ChatCompletionAgent> _reviewerAgents;
    private readonly ChatCompletionAgent _metaReviewerAgent;
    private readonly ChatCompletionAgent _criticAgent;
    private readonly Func<ChatMessageContent, bool> _terminationCondition;

    private readonly Kernel _kernel;

    public NestedChatMultiCriticMiddleware()
    {
        
    }

    public NestedChatMultiCriticMiddleware(
            ChatCompletionAgent writerAgent,
            IEnumerable<ChatCompletionAgent> reviewerAgents,
            ChatCompletionAgent metaReviewerAgent,
            ChatCompletionAgent criticAgent,
            Func<ChatMessageContent, bool> terminationCondition)
    {
        _writerAgent = writerAgent ?? throw new ArgumentNullException(nameof(writerAgent));
        _reviewerAgents = reviewerAgents ?? throw new ArgumentNullException(nameof(reviewerAgents));
        _metaReviewerAgent = metaReviewerAgent ?? throw new ArgumentNullException(nameof(metaReviewerAgent));
        _criticAgent = criticAgent ?? throw new ArgumentNullException(nameof(criticAgent));
        _terminationCondition = terminationCondition ?? throw new ArgumentNullException(nameof(terminationCondition));

        // Assuming all agents share the same Kernel
        _kernel = NestedChatMiddlewareWorkflowFactory.Kernel;
    }

    public async Task<ChatMessageContent> InvokeAsync(
        ChatMessageContent message,
        Func<ChatMessageContent, Task<ChatMessageContent>> next,
        CancellationToken cancellationToken = default)
    {
        // Only trigger when the last message is from the Writer
        if (!string.Equals(message?.AuthorName, _writerAgent.Name, StringComparison.OrdinalIgnoreCase))
        {
            // Proceed to the next middleware or agent
            return await next(message);
        }

        // Step 1: Invoke the Writer Agent
        ChatMessageContent writerResponse = await InvokeAgentAsync(_writerAgent, message, cancellationToken);

        // Step 2: Invoke all Reviewer Agents with the Writer's output
        var reviewTasks = _reviewerAgents.Select(reviewer => InvokeReviewerAsync(reviewer, writerResponse.Content, cancellationToken)).ToList();
        var reviewerFeedbacks = await Task.WhenAll(reviewTasks);

        // Step 3: Invoke the MetaReviewer Agent to aggregate feedback
        string aggregatedFeedbackPrompt = "Aggregate feedback from all reviewers and provide final suggestions on the writing.";
        ChatMessageContent aggregatedFeedback = await InvokeAgentAsync(_metaReviewerAgent, new ChatMessageContent(AuthorRole.User, aggregatedFeedbackPrompt), cancellationToken);

        // Step 4: Invoke the Critic Agent with the aggregated feedback
        ChatMessageContent criticResponse = await InvokeAgentAsync(_criticAgent, new ChatMessageContent(AuthorRole.User, aggregatedFeedback.Content), cancellationToken);

        // Step 5: Evaluate the Termination Strategy
        if (_terminationCondition(criticResponse))
        {
            // Optionally, you can mark the chat as complete or perform other actions
            // For simplicity, we'll just return the Critic's response
            return criticResponse;
        }

        // If not terminating, you might want to continue the conversation
        // Depending on your workflow, you can decide what to do next
        return criticResponse;
    }

    private async Task<ChatMessageContent> InvokeAgentAsync(ChatCompletionAgent agent, ChatMessageContent message, CancellationToken cancellationToken)
    {
        ChatMessageContent? response = null;

        // AgentChat invokation wrong
        // Should beAddChatMessage() and InvokeAsync() 
        //await foreach (var res in agent.InvokeAsync(message, cancellationToken))
        //{
        //    response = res;
        //    break; // Only take the first response
        //}

        if (response == null)
        {
            throw new InvalidOperationException($"Agent {agent.Name} did not produce a response.");
        }

        return response;
    }

    private async Task<ChatMessageContent> InvokeReviewerAsync(ChatCompletionAgent reviewer, string content, CancellationToken cancellationToken)
    {
        string reviewPrompt = $"Review the following content.\n\n{content}";
        ChatMessageContent reviewMessage = new ChatMessageContent(AuthorRole.User, reviewPrompt);
     
        return await InvokeAgentAsync(reviewer, reviewMessage, cancellationToken);
    }

    // Methods to create reviewer agents
    private Agent CreateSEOReviewerAgent()
    {
        const string seoReviewerInstructions = """
                You are an SEO reviewer, known for your ability to optimize content for search engines, ensuring that it ranks well and attracts organic traffic.
                Make sure your suggestion is concise (within 3 bullet points), concrete, and to the point.
                Begin the review by stating your role.
                """;

        return new ChatCompletionAgent
        {
            Instructions = seoReviewerInstructions,
            Name = "SEO_Reviewer",
            Kernel = NestedChatMiddlewareWorkflowFactory.Kernel,
        };
    }

    private Agent CreateLegalReviewerAgent()
    {
        const string legalReviewerInstructions = """
                You are a legal reviewer, known for your ability to ensure that content is legally compliant and free from any potential legal issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete, and to the point.
                Begin the review by stating your role.
                Also be aware of data privacy and GDPR compliance, which needs to be respected, so in doubt suggest the removal of PII information.
                Assume that the speakers have agreed to share their name and title, so there is no issue with sharing that in the article.
                """;

        return new ChatCompletionAgent
        {
            Instructions = legalReviewerInstructions,
            Name = "Legal_Reviewer",
            Kernel = NestedChatMiddlewareWorkflowFactory.Kernel,
        };
    }

    private Agent CreateEthicsReviewerAgent()
    {
        const string ethicsReviewerInstructions = """
                You are an ethics reviewer, known for your ability to ensure that content is ethically sound and free from any potential ethical issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete, and to the point.
                Begin the review by stating your role.
                """;

        return new ChatCompletionAgent
        {
            Instructions = ethicsReviewerInstructions,
            Name = "Ethics_Reviewer",
            Kernel = NestedChatMiddlewareWorkflowFactory.Kernel,
        };
    }

    private Agent CreateFactCheckerAgent()
    {
        string conferenceDescription = GetConferenceDescription();

        string factCheckerInstructions = $"""
                You are a FactChecker. Your job is to ensure that all facts mentioned in the article are accurate and derived from the provided source material.
                You will avoid any hallucinations and ensure that all the facts are cross-checked with the source material.

                Cross-check the content against the following source:

                {conferenceDescription}

                Make sure no invented information is included, and suggest corrections if any discrepancies are found.
                """;

        return new ChatCompletionAgent
        {
            Instructions = factCheckerInstructions,
            Name = "FactChecker",
            Kernel = NestedChatMiddlewareWorkflowFactory.Kernel,
        };
    }

    private Agent CreateStyleCheckerAgent()
    {
        const string styleCheckerInstructions = """
                You are a Style Checker. Your task is to ensure that the article is written in a proper style.
                Check that the writing style is positive, engaging, motivational, original, and funny.
                The phrases should not be too complex, and the tone should be friendly, casual, yet polite.
                Provide suggestions to improve the style if necessary.
                Provide ONLY suggestions, do not rewrite the content or write any part of the content in the style suggested; this is the work of the writer.
                """;

        return new ChatCompletionAgent
        {
            Instructions = styleCheckerInstructions,
            Name = "StyleChecker",
            Kernel = NestedChatMiddlewareWorkflowFactory.Kernel,
        };
    }

    private Agent CreateMetaReviewerAgent()
    {
        const string metaReviewerInstructions = """
                You are a meta reviewer. You aggregate and review the work of other reviewers and give final suggestions on the content.
                """;

        return new ChatCompletionAgent
        {
            Instructions = metaReviewerInstructions,
            Name = "Meta_Reviewer",
            Kernel = NestedChatMiddlewareWorkflowFactory.Kernel,
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
}