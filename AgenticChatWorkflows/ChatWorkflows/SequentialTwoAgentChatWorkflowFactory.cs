using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class SequentialTwoAgentChatWorkflowFactory
{
    private static Kernel? _kernel;
    public static Kernel Kernel => _kernel ??= CreateKernel();

    // Initialize Kernel lazily using environment variables
    private static Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();

        Kernel kernel = builder.AddAzureOpenAIChatCompletion(
                            deploymentName: EnvironmentWellKnown.DeploymentName,
                            endpoint: EnvironmentWellKnown.Endpoint,
                            apiKey: EnvironmentWellKnown.ApiKey)
                        .Build();

        return kernel;
    }

    public static IAgentGroupChat CreateChat()
    {
        // Create the TwoAgentChat configurations
        List<TwoAgentChatConfiguration> chatConfigurations = new()
        {
            CreateTranslationReviewConfig(),
            CreateLegalReviewConfig(),
            CreateFactCheckReviewConfig()
        };

        // Create the SequentialTwoAgentChat with these configurations
        SequentialTwoAgentChat sequentialTwoAgentChat = new SequentialTwoAgentChat(chatConfigurations);

        sequentialTwoAgentChat.IsComplete = false;

        return sequentialTwoAgentChat;
    }

    // Translation Worker + Critic Configuration
    private static TwoAgentChatConfiguration CreateTranslationReviewConfig()
    {
        var translatorAgent = CreateTranslatorAgent();
        var translationCriticAgent = CreateTranslationCriticAgent();

        return new TwoAgentChatConfiguration(
            workerAgent: translatorAgent,
            criticAgent: translationCriticAgent,
            maxIterations: 3,
            terminationKeyword: "APPROVE",
            carryover: true,
            resultProcessing: ResultProcessing.Replace
        );
    }

    // Legal Review Worker + Critic Configuration
    private static TwoAgentChatConfiguration CreateLegalReviewConfig()
    {
        var legalReviewerAgent = CreateLegalReviewerAgent();
        var legalCriticAgent = CreateLegalCriticAgent();

        return new TwoAgentChatConfiguration(
            workerAgent: legalReviewerAgent,
            criticAgent: legalCriticAgent,
            maxIterations: 3,
            terminationKeyword: "APPROVE",
            carryover: true,
            resultProcessing: ResultProcessing.Append
        );
    }

    // Fact-Checking Worker + Critic Configuration
    private static TwoAgentChatConfiguration CreateFactCheckReviewConfig()
    {
        var factCheckerAgent = CreateFactCheckerAgent();
        var factCheckCriticAgent = CreateFactCheckCriticAgent();

        return new TwoAgentChatConfiguration(
            workerAgent: factCheckerAgent,
            criticAgent: factCheckCriticAgent,
            maxIterations: 3,
            terminationKeyword: "APPROVE",
            carryover: true,
            resultProcessing: ResultProcessing.Append
        );
    }

    // Methods to Create Agents

    private static ChatCompletionAgent CreateTranslatorAgent()
    {
        const string translatorInstructions = @"
            You are a skilled translator. Translate the content provided to you accurately.
            Ensure the translation is precise and free from bias or subjective interpretation.
        ";

        return new ChatCompletionAgent
        {
            Instructions = translatorInstructions,
            Name = "Translator",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateTranslationCriticAgent()
    {
        const string translationCriticInstructions = @"
            You are a critic reviewing the translation. Your task is to find any incorrect or subjective points in the translation.
            Review the translation critically, and if it meets the standards, respond with 'APPROVE'.
        ";

        return new ChatCompletionAgent
        {
            Instructions = translationCriticInstructions,
            Name = "TranslationCritic",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateLegalReviewerAgent()
    {
        const string legalReviewerInstructions = @"
            You are a legal reviewer. Your job is to ensure that the content complies with legal regulations.
            Ensure the content is clear, legally accurate, and free of any potential legal issues.
            you are only to check the last provided translation, in English.
            Elaborate your output in markdown format as output and suggest improvements.
        ";

        return new ChatCompletionAgent
        {
            Instructions = legalReviewerInstructions,
            Name = "LegalReviewer",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateLegalCriticAgent()
    {
        const string legalCriticInstructions = @"
            You are a critic reviewing the legal review. Your task is to find any inaccuracies or subjective interpretations in the legal review.
            Review the legal analysis carefully, and if it meets the standards, respond with 'APPROVE'.
        ";

        return new ChatCompletionAgent
        {
            Instructions = legalCriticInstructions,
            Name = "LegalCritic",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateFactCheckerAgent()
    {
        const string factCheckerInstructions = @"
            You are a fact checker. Your task is to verify that all the factual claims in the content are accurate.
            you are only to check the last provided translation, in English.
            Elaborate your output in markdown format as output and suggest improvements.
            Cross-check the information with trusted sources and flag any discrepancies.
        ";

        return new ChatCompletionAgent
        {
            Instructions = factCheckerInstructions,
            Name = "FactChecker",
            Kernel = Kernel,
        };
    }

    private static ChatCompletionAgent CreateFactCheckCriticAgent()
    {
        const string factCheckCriticInstructions = @"
            You are a critic reviewing the fact-checking process. Your task is to find any missed points or subjective conclusions in the fact check.
            If the fact-check is thorough and accurate, respond with 'APPROVE'.
        ";

        return new ChatCompletionAgent
        {
            Instructions = factCheckCriticInstructions,
            Name = "FactCheckCritic",
            Kernel = Kernel,
        };
    }
}