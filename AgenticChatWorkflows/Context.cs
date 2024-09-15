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
    private static string _concept = "This is the concept workflow context";
    public static string Concept
    {
        get => _concept;
        set
        {
            if (_concept != value)
            {
                _concept = value;
                NotifyPropertyChanged(nameof(Concept));
            }
        }
    }

    private static string _characters = "This is the characters workflow context";
    public static string Characters
    {
        get => _characters;
        set
        {
            if (_characters != value)
            {
                _characters = value;
                NotifyPropertyChanged(nameof(Characters));
            }
        }
    }

    private static string _world = "This is the world workflow context";
    public static string World
    {
        get => _world;
        set
        {
            if (_world != value)
            {
                _world = value;
                NotifyPropertyChanged(nameof(World));
            }
        }
    }

    private static string _plot = "This is the plot workflow context";
    public static string Plot
    {
        get => _plot;
        set
        {
            if (_plot != value)
            {
                _plot = value;
                NotifyPropertyChanged(nameof(Plot));
            }
        }
    }

    private static string _structure = "This is the structure workflow context";
    public static string Structure
    {
        get => _structure;
        set
        {
            if (_structure != value)
            {
                _structure = value;
                NotifyPropertyChanged(nameof(Structure));
            }
        }
    }

    // Method to trigger the PropertyChanged event
    private static void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(propertyName);
    }
}
