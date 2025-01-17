using RuleGrid.Utilities;

namespace RuleGrid.Exceptions;

public class RuleGridValidationException : Exception
{
    public RuleGridValidationException(string fieldName, string fieldDisplayName, string message) : base(
        string.Format(message, fieldDisplayName))
    {
        FieldName = StringUtility.FirstCharToUpperAsSpan(fieldName);
        FieldDisplayName = fieldDisplayName;
    }

    public string FieldName { get; }
    public string FieldDisplayName { get; }
}