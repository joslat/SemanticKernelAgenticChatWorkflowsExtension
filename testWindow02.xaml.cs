using AgenticChatWorkflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AgenticChatWorkflows;

/// <summary>
/// Interaction logic for testWindow02.xaml
/// </summary>
public partial class testWindow02 : Window
{

    private CancellationTokenSource _cts;  // For debouncing

    // Store the widths for each expander
    private Dictionary<string, double> expanderWidths = new Dictionary<string, double>
    {
        { "Concept", 200 },
        { "Characters", 200 },
        { "World", 200 },
        { "Plot", 200 },
        { "Structure", 200 },
        { "Configuration", 200 }
    };
    private readonly double _collapsedWidth = 56; // Width for the collapsed header
    private AgenticWorkflowModel? agenticWorkflowModel;

    public testWindow02()
    {
        InitializeComponent();
        _cts = new CancellationTokenSource();
        agenticWorkflowModel = new AgenticWorkflowModel(this);

        // Default selected workflow is set to ConceptWorkflow
        agenticWorkflowModel.SelectedWorkflow = WorkflowType.CodeCrafterAgentChatWorkflow;

        this.DataContext = agenticWorkflowModel;
    }

    private void Expander_Collapsed(object sender, RoutedEventArgs e)
    {
        var expander = sender as Expander;
        if (expander == null) return;

        // Locate the column associated with the expander
        var column = FindColumnForExpander(expander);
        if (column != null)
        {
            // Store the current width before collapsing
            double currentWidth = expander.ActualWidth;
            StoreExpanderWidth(expander.Header.ToString(), currentWidth);

            // Set the column width to the collapsed width
            column.Width = new GridLength(_collapsedWidth);
        }
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        var expander = sender as Expander;
        if (expander == null) return;

        // Locate the column and retrieve the stored width for the expander
        var column = FindColumnForExpander(expander);
        if (column != null)
        {
            // Restore the previously stored width after expanding
            double storedWidth = expanderWidths[expander.Header.ToString()];
            column.Width = new GridLength(storedWidth);
        }
    }

    // Function to store the width for each expander in the dictionary
    private void StoreExpanderWidth(string expanderHeader, double width)
    {
        if (expanderWidths.ContainsKey(expanderHeader))
        {
            expanderWidths[expanderHeader] = width;
        }
    }

    // Function to return the corresponding column for the expander
    private ColumnDefinition FindColumnForExpander(Expander expander)
    {
        switch (expander.Header.ToString())
        {
            case "Configuration":
                return PanelColumn1;
            case "Concept":
                return ExpanderColumnConcept;
            case "Characters":
                return ExpanderColumnCharacters;
            case "World":
                return ExpanderColumnWorld;
            case "Plot":
                return ExpanderColumnPlot;
            case "Structure":
                return ExpanderColumnStructure;
            default:
                return null;
        }
    }

    private async void AskButton_Click(object sender, RoutedEventArgs e)
    {
        // Disable the Ask button
        AskButton.IsEnabled = false;

        // Call the askQuestion method
        await agenticWorkflowModel?.askQuestion();

        // Re-enable the Ask button after the process is finished
        AskButton.IsEnabled = true;

    }

    private void QuestionBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            AskButton_Click(sender, e);
        }
    }

    public void AgenticWorkflow_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        agenticWorkflowModel?.InitializeChatWorkflow();  // Re-initialize the chat workflow based on the selected workflow
    }

    // Debounce the TextChanged event
    public async void ConceptRTB_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Cancel the previous task if a new TextChanged event occurs
        _cts.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            // Wait for 300ms (adjust the delay as needed)
            await Task.Delay(400, _cts.Token);

            // Update the Concept property after the delay
            if (ConceptRTB != null)
            {
                TextRange textRange = new TextRange(ConceptRTB.Document.ContentStart, ConceptRTB.Document.ContentEnd);
                Context.Concept = textRange.Text.Trim();  // Sync RichTextBox content with Concept property at source
            }
        }
        catch (TaskCanceledException)
        {
            // Task was canceled due to new input, so we don't update anything
        }
    }
}
