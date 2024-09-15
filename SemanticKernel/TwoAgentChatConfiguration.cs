using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

public class TwoAgentChatConfiguration
{
    public ChatCompletionAgent WorkerAgent { get; set; }
    public ChatCompletionAgent CriticAgent { get; set; }
    public int MaxIterations { get; set; }
    public string? TerminationKeyword { get; set; }
    public bool Carryover { get; set; }
    public ResultProcessing ResultProcessing { get; set; }


    public TwoAgentChatConfiguration(
        ChatCompletionAgent workerAgent,
        ChatCompletionAgent criticAgent,
        int maxIterations,
        string terminationKeyword,
        bool carryover,
        ResultProcessing resultProcessing)
    {
        WorkerAgent = workerAgent;
        CriticAgent = criticAgent;
        MaxIterations = maxIterations;
        TerminationKeyword = terminationKeyword;
        Carryover = carryover;
        ResultProcessing = resultProcessing;
    }
}