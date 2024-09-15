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

class AgenticWorkflowModel : INotifyPropertyChanged
{
    testWindow02? mainWindow;

    private int _CharacterLimit = 2000;
    public IAgentGroupChat? ChatWorkflow { get; private set; }

    public int CharacterLimit
    {
        get { return _CharacterLimit; }
        set
        {
            if (_CharacterLimit != value)
            {
                _CharacterLimit = value;
                OnPropertyChanged("CharacterLimit");
            }
        }
    }

    public string Facts
    {
        get { return Context.Facts; }
        set
        {
            if (Context.Facts != value)
            {
                Context.Facts = value;
                OnPropertyChanged("Facts");
            }
        }
    }

    public string Concept
    {
        get { return Context.Code; }
        set
        {
            if (Context.Code != value)
            {
                Context.Code = value;
                UpdateConceptRTB();  // Manually update RichTextBox content
                OnPropertyChanged("Concept");
            }
        }
    }

    private Dictionary<WorkflowType, string> _workflowOptions;
    private WorkflowType _selectedWorkflow;

    public Dictionary<WorkflowType, string> WorkflowOptions
    {
        get => _workflowOptions;
        set
        {
            _workflowOptions = value;
            OnPropertyChanged(nameof(WorkflowOptions));
        }
    }

    public WorkflowType SelectedWorkflow
    {
        get => _selectedWorkflow;
        set
        {
            _selectedWorkflow = value;
            OnPropertyChanged(nameof(SelectedWorkflow));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string _Question = "Does your service offer video generative AI?";
    public string Question
    {
        get { return _Question; }
        set
        {
            if (_Question != value)
            {
                _Question = value;
                OnPropertyChanged("Question");
            }
        }
    }

    string? QuestionAnswererPrompt;
    string? AnswerCheckerPrompt;
    string? LinkCheckerPrompt;
    string? ManagerPrompt;

    public AgenticWorkflowModel(testWindow02 mainWindow)
    {
        this.mainWindow = mainWindow;
        PopulateWorkflowOptions();
        InitializeChatWorkflow();
        UpdateContext();
    }

    // Method to populate the WorkflowOptions dictionary
    private void PopulateWorkflowOptions()
    {
        WorkflowOptions = new Dictionary<WorkflowType, string>
        {
            { WorkflowType.TestWorkflow, "Test Workflow" },
            { WorkflowType.CodeCrafterAgentChatWorkflow, "Code Crafter Single Agent Workflow" },
            { WorkflowType.TestV2Workflow, "Test V2 Workflow" },
            { WorkflowType.TwoAgentChatWorkflow, "Two Agent Workflow" },
            { WorkflowType.SequentialAgentChatWorkflow, "Sequential Agent Workflow" },
            { WorkflowType.SequentialTwoAgentChatWorkflow, "Sequential Two Agent Workflow" },
            { WorkflowType.NestedChatWithGroupAgentChatWorkflow, "Nested Chat with Group Agent Chat Agent Workflow" },
        };
    }

    public void InitializeChatWorkflow()
    {
        switch (SelectedWorkflow)
        {
            case WorkflowType.TestWorkflow:
                ChatWorkflow = TestWorkflowChatFactory.CreateChat(CharacterLimit);
                break;

            case WorkflowType.TestV2Workflow:
                ChatWorkflow = TestV2ChatWorkflowFactory.CreateChat(CharacterLimit);
                break;

            case WorkflowType.TwoAgentChatWorkflow:
                ChatWorkflow = TwoAgentChatWorkflowFactory.CreateChat(CharacterLimit);
                break;

            case WorkflowType.SequentialAgentChatWorkflow:
                ChatWorkflow = SequentialAgentChatWorkflowFactory.CreateChat(CharacterLimit);
                break;

            case WorkflowType.SequentialTwoAgentChatWorkflow:
                ChatWorkflow = SequentialTwoAgentChatWorkflowFactory.CreateChat();
                break;

            case WorkflowType.NestedChatWithGroupAgentChatWorkflow:
                ChatWorkflow = NestedChatWithGroupAgentChatWorkflowFactory.CreateChat();
                break;                

            case WorkflowType.CodeCrafterAgentChatWorkflow:
                ChatWorkflow = CodeCrafterWorkflowChatFactory.CreateChat(CharacterLimit);
                break;

            default:
                // Handle default case if no workflow matches
                updateResponseBox("Error", "No valid workflow selected.");
                ChatWorkflow = null;
                break;
        }
    }

    public async Task askQuestion()
    {
        if (ChatWorkflow == null)
        {
            updateResponseBox("Error", "No chat workflow is initialized for the selected workflow.");
            return;
        }

        string input = Question;
        ChatWorkflow.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));

        updateResponseBox("Question", input);

        string finalAnswer = "";

        await foreach (var content in ChatWorkflow.InvokeAsync())
        {
            Color color;
            switch (content.AuthorName)
            {
                case "QuestionAnswererAgent":
                    color = Colors.Black;
                    finalAnswer = content.Content;  // Assume last time it's called, it's the final answer
                    break;
                case "AnswerCheckerAgent":
                    color = Colors.Blue;
                    break;
                case "LinkCheckerAgent":
                    color = Colors.DarkGoldenrod;
                    break;
                case "ManagerAgent":
                    color = Colors.DarkGreen;
                    break;
                case "IdeaCrafterAgent":
                    color = Colors.Yellow;
                    break;
                case "ArtDirector":
                    color = Colors.DarkRed;
                    break;
                case "CopyWriter":
                    color = Colors.DarkBlue;
                    break;
                
                default:
                    color = Colors.DarkSlateBlue;
                    break;
            }

            updateResponseBox(content.AuthorName, content.Content, color);
        }
    }

    public void updateResponseBox(string sender, string response)
    {
        updateResponseBox(sender, response, Colors.Black);
    }

    public void updateResponseBox(string sender, string response, Color color)
    {
        //Update mainWindow.ResponseBox to add the sender in bold, a colon, a space, and the response in normal text
        Paragraph paragraph = new Paragraph();
        Bold bold = new Bold(new Run(sender + ": "));
        
        bold.Foreground = new SolidColorBrush(color);
        
        paragraph.Inlines.Add(bold);
        Run run = new Run(response);
        paragraph.Inlines.Add(run);
        mainWindow.ResponseBox.Document.Blocks.Add(paragraph);

        // Scroll to the end after adding new content
        ScrollToEnd(mainWindow.ResponseBox);
    }

    private void ScrollToEnd(RichTextBox richTextBox)
    {
        if (richTextBox != null)
        {
            // Use Dispatcher to ensure it's invoked on the UI thread
            richTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                richTextBox.ScrollToEnd();
            }));
        }
    }

    #region Context changes and updates
    private void UpdateContext()
    {
        Context.PropertyChanged -= OnContextPropertyChanged;

        UpdateConceptRTB();
        // Subscribe to static Context property changes
        Context.PropertyChanged += OnContextPropertyChanged;
    }

    // Update RichTextBox with the Concept content
    //public void UpdateConceptRTB()
    //{
    //    if (mainWindow != null && mainWindow.ConceptRTB != null)
    //    {
    //        mainWindow.ConceptRTB.Document.Blocks.Clear();
    //        mainWindow.ConceptRTB.Document.Blocks.Add(new Paragraph(new Run(Concept)));
    //    }
    //}
    public void UpdateConceptRTB()
    {
        if (mainWindow != null && mainWindow.ConceptRTB != null)
        {
            // Check if we're on the UI thread
            if (mainWindow.Dispatcher.CheckAccess())
            {
                // We are on the UI thread, so we can directly update the RichTextBox
                mainWindow.ConceptRTB.Document.Blocks.Clear();
                mainWindow.ConceptRTB.Document.Blocks.Add(new Paragraph(new Run(Concept)));
            }
            else
            {
                // We're not on the UI thread, so we must use the Dispatcher to update the UI
                mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainWindow.ConceptRTB.Document.Blocks.Clear();
                    mainWindow.ConceptRTB.Document.Blocks.Add(new Paragraph(new Run(Concept)));
                }));
            }
        }
    }



    #endregion

    private void OnContextPropertyChanged(string propertyName)
    {
        // Raise the property changed event for the respective property
        UpdateConceptRTB();
        OnPropertyChanged(propertyName);
        
    }

    ~AgenticWorkflowModel()
    {
        // Unsubscribe from event to avoid memory leaks
        Context.PropertyChanged -= OnContextPropertyChanged;
    }

}
