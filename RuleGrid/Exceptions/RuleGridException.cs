namespace RuleGrid.Exceptions;

public class RuleGridException : Exception
{
    public RuleGridException(string message) : base(message)
    {
    }
    public RuleGridException(int code, string message) : base($"Exception with code:{code} was thrown, {message}")
    {
        Code = code;
    }

    public RuleGridException(string message, object result) : base(message)
    {
        Result = result;
    }

    public object Result { get; }
    public int? Code { get; }
}