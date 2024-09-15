using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;

namespace AgenticChatWorkflows;

public class StringToFlowDocumentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            FlowDocument doc = new FlowDocument();
            doc.Blocks.Add(new Paragraph(new Run(stringValue)));
            return doc;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FlowDocument document)
        {
            TextRange textRange = new TextRange(document.ContentStart, document.ContentEnd);
            return textRange.Text.Trim();  // return string representation of FlowDocument
        }
        return string.Empty;
    }
}