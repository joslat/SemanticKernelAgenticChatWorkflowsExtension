using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgenticChatWorkflows;

public static class Context
{
    // Event to notify when any property changes
    public static event Action<string>? PropertyChanged;

    private static string _facts = "Microsoft Azure AI";
    public static string Facts
    {
        get => _facts;
        set
        {
            if (_facts != value)
            {
                _facts = value;
                NotifyPropertyChanged(nameof(Facts));
            }
        }
    }

    // Workflow-specific properties with change notification
    private static string _code = "";
    public static string Code
    {
        get => _code;
        set
        {
            if (_code != value)
            {
                _code = value;
                NotifyPropertyChanged(nameof(Code));
            }
        }
    }

    // Method to trigger the PropertyChanged event
    private static void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(propertyName);
    }
}
