using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticChatWorkflows;

public enum WorkflowType
{
    TestWorkflow,
    TestV2Workflow,
    TwoAgentChatWorkflow,
    SequentialAgentChatWorkflow,
    SequentialTwoAgentChatWorkflow,
    NestedChatWithGroupAgentChatWorkflow,
    CodeCrafterAgentChatWorkflow,
}