# SemanticKernelAgenticChatWorkflowsExtension
Playing around with extending Semantic Kernel and implementing several agentic chat workflow patterns.

I wanted to implement some advanced patterns with Semantic Kernel and got stuck with some things I could not extend.
Essentially some classes are sealed and do not have any interfaces to implement.

I have made an issue and a PR to get this in a better, more extensible state.
- Issue: https://github.com/microsoft/semantic-kernel/issues/8719
- PR: https://github.com/microsoft/semantic-kernel/pull/8720

## Semantic Kernel extensions
I have implemented some interfaces so to extend Semantic Kernel and implement several agentic chat workflow patterns.
- IAgentGroupChat & IAgentChat so I can create customized chat workflows.
- Implemented a wrapper for the AgentGroupChat, AgentGroupChatEx so I can inject any kind of IAgentGroupChat and extend it
- Created a AgentGroupChatExt which wraps the AgentGroupChat and implements IAgentGroupChat
- Created the BaseAgentGroupChat which is a base class for other AgentGroupChat implementations
- Implemented the SequAgentChat which is a sequential chat workflow.
- Implemented the TwoAgentChat which is a chat between two agents managed through code (fast).
- Implemented a SequeentialTwoAgentChat which is a sequential chat between several two agent workflows, through code (fast).
- Tried to implement a Middleware pattern as it is in AutoGen.NET but did not fully work. Got stuck with some things I could not extend...

## Application
I have created a WPF application based on a sample from Marco Casalaina exhibited in the Cozy AI Kitchen series from John Maeda.
Sources:
https://www.youtube.com/watch?v=7VCkdxKNBl4
https://techcommunity.microsoft.com/t5/ai-ai-platform-blog/the-future-of-ai-exploring-multi-agent-ai-systems/ba-p/4226593
https://github.com/mcasalaina/QuestionnaireMultiagent

I have extended a bit to plug dynamically different chat workflows and to be able to change the chat workflow on the fly.
Also improved the UI to my liking and needs.

## Idea
I'd like to thank Chris Rickman, https://github.com/crickman, a Microsoft Principal Software Engineer working on the Semantic Kernel team, 
for the great discussions and his suggestions on how to extend Semantic Kernel which leaded to this repository.


## Current implementations (in UI, and workflow providers)
- Code Crafter workflow (single  - agent) - to improve or program some code
- Two Agent Chat Workflow - chat between two agents
- - Sequential Agent Chat Workflow - sequential chat between agents
- Sequential Two Agent Chat Workflow - sequential chat between a set of a number of two agent chats
- TestWorkflow - a test workflow to test the chat workflows with a "normal chat of X Agents"
- TestV2ChatWorkflow - IAgentGroupChat Chat group with termination function - just based on the AgentGroupChatExt wrapper