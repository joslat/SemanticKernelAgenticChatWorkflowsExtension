## TODO

### 1. Semantic Kernel extension
Goal: Implement abstractions that allow To extend the kernel on Agents as well as on the Chat types. 
The main goal is to extend the agent class and most important, the chat class.
I want to add a chat class that can implement a Sequential chat workflow and a Parallel chat workflow.
I want also to enable the AgentGroupChat into receiving such an abstraction so I can inject into it any kind of custom IAgentGroupChat.

### Tasks:
- create a new branch repository for the hackathon, private. 
- Share it with the team.
- add Monaco to view code
- create a worker -set of multi critic worflow that "chains" the agents" and a critic that receives all the feedback summarizerd and decides if to iterate again or not.
- add treeview to view code structure
- change the code view from the selection on the treeview
- load a project or folder into the treview
- implement tools: static code analysis, build code, test code (run tests), .NET Interactive (to run code)
- think in other agent workflows that can be implemented and try to implement them

## DONE
- upload the code to github (as oss in an open project) - for fairness and share it with the community
- - Implement the IAgentGroupChat interface
- Implement the IAgentChat interface
- Implement the IAgentChatSequential interface
- Change the AgentGroupChat to implement IAgentGroupChat
- Change AggregatorAgent to receive an IAgentGroupChat
- Change the AgentChat to implement IAgentChat
- Implement AgentSequentialChat implementing IAgentSequentialChat
- Implement AgentTwoAgentChat implementing IAgentGroupChat
- build the tests for the new abstractions
- implement the SequentialTwoAgentChat
- implement a NestedChatWithSequentialAgentChatFactory. (done but did not fully work...)
