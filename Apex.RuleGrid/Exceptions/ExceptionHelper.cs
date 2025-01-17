using Apex.RuleGrid.Constants;
using Apex.RuleGrid.Exceptions;
using System.Runtime.CompilerServices;

namespace Apex.RuleGrid.Exceptions;

public static class ExceptionHelper
{
    public static void ThrowIfNullOrWhiteSpace<TObject>(TObject @object, string fieldPersianName,
        [CallerArgumentExpression("object")]
        string fieldName = "")
    {
        if (@object is string str && string.IsNullOrWhiteSpace(str))
            throw new RuleGridValidationException(fieldName, fieldPersianName, ErrorMessages.Required);
    }
}