using AgenticChatWorkflows.AutoGen.Agents;
using AgenticChatWorkflows.Helpers;
using AutoGen.Core;
using Azure.AI.OpenAI;
using Google.Rpc;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticChatWorkflows.AutoGen;

public static class AutoGenChatWorkflow_AskForFeedback
{
    public static async Task<string> Execute(string article)
    {
        // Use AzureOpenAI
        string model;
        AzureOpenAIClient client;
        WellKnown.GetAutoGenModelAndConnectionToLLM(out model, out client);

        // Agent creation
        var CriticWrapperAgent = AutoGen_CriticWrapperAgent.CreateAgent(article);
        var EthicsReviewerAgent = AutoGen_EthicsReviewerAgent.CreateAgent();
        var LegalReviewerAgent = AutoGen_LegalReviewerAgent.CreateAgent();
        var SEOReviewerAgent = AutoGen_SEOReviewerAgent.CreateAgent();
        var StyleCheckerAgent = AutoGen_StyleCheckerAgent.CreateAgent();
        var MetaReviewerAgent = AutoGen_MetaReviewerAgent.CreateAgent();

        // Middleware creation
        var middleware = new NestedChatReviewerMiddleware(
            CriticWrapperAgent,
            EthicsReviewerAgent,
            LegalReviewerAgent,
            SEOReviewerAgent,
            StyleCheckerAgent,
            MetaReviewerAgent);

        var nestedChatCriticWrapperAgent = AutoGen_CriticWrapperAgent.CreateAgent(article);

        // Register the middleware and setup message printing
        var middlewareAgent = nestedChatCriticWrapperAgent
            .RegisterMiddleware(middleware)
            .RegisterPrintMessage();

        // https://microsoft.github.io/autogen-for-net/articles/Agent-overview.html
        var message = new TextMessage(Role.User, $"The code to review is: {article}");

        IMessage reply = await middlewareAgent.GenerateReplyAsync([message]);

        return reply.GetContent();
    }
}

public class NestedChatReviewerMiddleware : IMiddleware
{
    private readonly IAgent CriticWrapperAgent;
    private readonly IAgent EthicsReviewerAgent;
    private readonly IAgent LegalReviewerAgent;
    private readonly IAgent SEOReviewerAgent;
    private readonly IAgent StyleCheckerAgent;
    private readonly IAgent MetaReviewerAgent;

    public NestedChatReviewerMiddleware(
        IAgent criticWrapperAgent,
        IAgent ethicsReviewerAgent,
        IAgent legalReviewerAgent,
        IAgent seoReviewerAgent,
        IAgent styleCheckerAgent,
        IAgent metaReviewerAgent)
    {
        CriticWrapperAgent = criticWrapperAgent;
        EthicsReviewerAgent = ethicsReviewerAgent;
        LegalReviewerAgent = legalReviewerAgent;
        SEOReviewerAgent = seoReviewerAgent;
        StyleCheckerAgent = styleCheckerAgent;
        MetaReviewerAgent = metaReviewerAgent;
    }

    public string? Name => nameof(NestedChatReviewerMiddleware);

    public async Task<IMessage> InvokeAsync(
        MiddlewareContext context,
        IAgent critic,
        CancellationToken cancellationToken = default)
    {
        var messageToReview = context.Messages.Last();
        var reviewPrompt = $"""
            Review the following article:
            {messageToReview.GetContent()}
            """;

        var criticReviewTask = critic.SendAsync(
            receiver: CriticWrapperAgent,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync()
            .AsTask();

        var ethicsReviewTask = critic.SendAsync(
            receiver: EthicsReviewerAgent,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync()
            .AsTask();

        var legalReviewTask = critic.SendAsync(
            receiver: LegalReviewerAgent,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync()
            .AsTask();

        var seoReviewTask = critic.SendAsync(
            receiver: SEOReviewerAgent,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync()
            .AsTask();

        var styleReviewTask = critic.SendAsync(
            receiver: StyleCheckerAgent,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync()
            .AsTask();

        // Await all review tasks to enable parallel execution
        await Task.WhenAll(
            criticReviewTask,
            ethicsReviewTask,
            legalReviewTask,
            seoReviewTask,
            styleReviewTask);

        var criticReview = await criticReviewTask;
        var ethicsReview = await ethicsReviewTask;
        var legalReview = await legalReviewTask;
        var seoReview = await seoReviewTask;
        var styleReview = await styleReviewTask;

        // Combine reviews from all agents
        var allReviews = criticReview
            .Concat(ethicsReview)
            .Concat(legalReview)
            .Concat(seoReview)
            .Concat(styleReview);

        var metaReview = await critic.SendAsync(
            receiver: MetaReviewerAgent,
            message: "Aggregate feedback from all reviewers and give final suggestions on the article.",
            chatHistory: allReviews,
            maxRound: 1)
            .ToListAsync();

        var lastReview = metaReview.Last();
        lastReview.From = critic.Name;

        // return the summarized reviews
        return lastReview;
    }
}
