using Apex.RuleGrid.Exceptions;
using System.Net;
using System.Text.Json.Serialization;

namespace Apex.RuleGrid.Models;

public class StandardApiBaseModel
{
    public int Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResponseErrorType? ErrorType { get; set; }

    public IDictionary<string, string[]> ValidationErrors { get; set; }
    public string ErrorMessage { get; set; }
    public string TraceId { get; set; }
    public string ActionId { get; set; }
    public bool IsOk => Status == (int)HttpStatusCode.OK;
}

public class StandardApiModel : StandardApiBaseModel
{
    public object Result { get; set; }
}

public class StandardApiModel<TResult> : StandardApiBaseModel
{
    public TResult? Result { get; set; }
}