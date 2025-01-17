namespace RuleGrid.Exceptions;

public enum ResponseErrorType : byte
{
    GeneralError = 0,
    ValidationError,
    AggregateException,
    AuthorizationException
}