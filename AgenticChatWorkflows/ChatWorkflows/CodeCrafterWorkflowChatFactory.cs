using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using System.Net;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Reflection.Metadata;
using System.Windows.Media;


#pragma warning disable SKEXP0110, SKEXP0001, SKEXP0050, CS8600, CS8604

namespace AgenticChatWorkflows;

public static class CodeCrafterWorkflowChatFactory
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
        KernelPlugin updateConceptPlugin = KernelPluginFactory.CreateFromType<UpdateCode>();
        kernel.Plugins.Add(updateConceptPlugin);

        return kernel;
    }

    public static IAgentGroupChat CreateChat(int characterLimit = 2000, int maxIterations = 1)
    {
        string projectDetails = Context.Facts;

        // Main coding agent with combined responsibilities
        ChatCompletionAgent CodeCrafterAgent = new()
        {
            Instructions = $"""
                Your name is CodeCrafterAgent, an expert in software development, clean coding principles, and code legibility.

                # Context
                ## Software Project
                The user is developing a software application and needs assistance in crafting the main components, including architecture, algorithms, writing clean, efficient code, and ensuring the code follows best practices.

                ## Task
                Your task is to help the user design and write high-quality, maintainable code. You will guide the user through the following tasks:
                
                ### 1. **Architecture**
                Help the user define the **software architecture** for the project.
                - Ask: "What is the core functionality and what architecture suits the project best (e.g., MVC, microservices, monolithic)?"
                - Provide recommendations based on scalability, maintainability, and performance.
                - **Request feedback**: Ask if the user is satisfied with the architecture before proceeding.

                ### 2. **Algorithm Design**
                Assist the user in designing **algorithms** for core functionalities.
                - Ask: "What problem are we solving? What is the most efficient way to solve it?"
                - Offer examples of algorithms (e.g., sorting, searching, dynamic programming).
                - **Request feedback**: Confirm that the user agrees with the proposed algorithms or if they would like to make adjustments.

                ### 3. **Clean Code Principles**
                Review the code for **clean coding principles**. Ensure the code adheres to best practices such as DRY (Don't Repeat Yourself), SRP (Single Responsibility Principle), and is easily maintainable.
                - Suggest improvements where necessary, especially focusing on modularity, readability, and maintainability.
                - **Request feedback**: Ask the user if they agree with the changes or if additional refactoring is needed.

                ### 4. **Naming Conventions**
                Ensure the code follows proper **naming conventions** for variables, functions, and classes.
                - Review the names to make sure they are clear, meaningful, and follow best practices for readability.
                - **Request feedback**: Ask the user if the naming conventions align with their preferences or if they want adjustments.

                ### 5. **Code Legibility**
                Ensure the code is **legible** for future developers. Focus on proper formatting, spacing, indentation, and consistency.
                - Suggest formatting improvements if the code is hard to read or lacks structure.
                - **Request feedback**: Confirm with the user if the code is legible enough for their team or future developers.

                ### 6. **Code Review and Final Approval**
                Conduct a final **code review** to ensure all the above aspects are met.
                - Ask: "Is the code following the project standards? Is there any redundancy or areas for optimization?"
                - Ensure the code adheres to best practices for security, performance, and maintainability.
                - **Request feedback**: Before finalizing the review, ensure the user approves the code quality.

                ## Update the Code
                Once the user is satisfied with the final version, use the **UpdateCode tool** to finalize the code.
                - **Ensure that the user has reviewed and approved** the final version before updating.


                ## Existing code
                The existing code is:
                {Context.Code}
                """,

            Name = "CodeCrafterAgent",
            Kernel = Kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                }),
        };

        IAgentGroupChat chat = new AgentGroupChatExt(CodeCrafterAgent)
        {
            ExecutionSettings = new()
            {
                TerminationStrategy = new ApprovalTerminationStrategy()
                {
                    Agents = [CodeCrafterAgent],
                    MaximumIterations = maxIterations,
                }
            }
        };

        return chat;
    }

    private sealed class UpdateCode
    {
        [KernelFunction, Description("Updates the Code with a provided code segment.")]
        public void UpdateTheCode(
            [Description("The code segment.")] string code)
        {
            Context.Code = code;
        }
    }
}


// Based on the "base code", achieve the goal.  Let's go back to basics with this simple card game: war! Let's go back to basics with this simple card game: war!Your goal is to write a program which finds out which player is the winner for a given card distribution of the "war" game. War is a card game played between two players. Each player gets a variable number of cards of the beginning of the game: that's the player's deck. Cards are placed face down on top of each deck. Step 1 : the fight At each game round, in unison, each player reveals the top card of their deck – this is a "battle" – and the player with the higher card takes both the cards played and moves them to the bottom of their stack. The cards are ordered by value as follows, from weakest to strongest: 2, 3, 4, 5, 6, 7, 8, 9, 10, J, Q, K, A.   Step 2 : war If the two cards played are of equal value, then there is a "war". First, both players place the three next cards of their pile face down. Then they go back to step 1 to decide who is going to win the war (several "wars" can be chained). As soon as a player wins a "war", the winner adds all the cards from the "war" to their deck. Special cases If a player runs out of cards during a "war" (when giving up the three cards or when doing the battle), then the game ends and both players are placed equally first. The test cases provided in this puzzle are built in such a way that a game always ends (you do not have to deal with infinite games) Each card is represented by its value followed by its suit: D, H, C, S. For example: 4H, 8C, AS. When a player wins a battle, they put back the cards at the bottom of their deck in a precise order. First the cards from the first player, then the one from the second player (for a "war", all the cards from the first player then all the cards from the second player).  Start from the existing code provided


// Copy this in code view

//using System;
//using System.Linq;
//using System.IO;
//using System.Text;
//using System.Collections;
//using System.Collections.Generic;

///**
// * Auto-generated code below aims at helping you parse
// * the standard input according to the problem statement.
// **/
//class Solution
//{
//    static void Main(string[] args)
//    {
//        int n = int.Parse(Console.ReadLine()); // the number of cards for player 1
//        for (int i = 0; i < n; i++)
//        {
//            string cardp1 = Console.ReadLine(); // the n cards of player 1
//        }
//        int m = int.Parse(Console.ReadLine()); // the number of cards for player 2
//        for (int i = 0; i < m; i++)
//        {
//            string cardp2 = Console.ReadLine(); // the m cards of player 2
//        }

//        // Write an answer using Console.WriteLine()
//        // To debug: Console.Error.WriteLine("Debug messages...");

//        Console.WriteLine("PAT");
//    }
//}