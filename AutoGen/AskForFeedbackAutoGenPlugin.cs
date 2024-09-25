using System.ComponentModel;
using AutoGen.Core;
using Microsoft.SemanticKernel;
using OpenAI;

namespace AgenticChatWorkflows.AutoGen;

public class askForFeedbackAutoGenPlugin
{
    [KernelFunction, Description("Performs a code review through different experts")]
    public async Task<string> AskForFeedback(string article)
    {
        string articleReview = await AutoGenChatWorkflow_AskForFeedback.Execute(article);

        return articleReview;
    }
}
