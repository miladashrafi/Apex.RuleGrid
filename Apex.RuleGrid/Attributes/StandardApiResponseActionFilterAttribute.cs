using Apex.RuleGrid.Exceptions;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Apex.RuleGrid.Models;
using System.Net;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Apex.RuleGrid.Attributes;

public class StandardApiResponseActionFilterAttribute(ILogger<StandardApiResponseActionFilterAttribute> logger) : ActionFilterAttribute
{
    private readonly ILogger<StandardApiResponseActionFilterAttribute> _logger = logger;

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var actionId = context.ActionDescriptor.Id;
        var actionName = context.ActionDescriptor.DisplayName;

        _logger.Debug("Processing action response for {ActionName} with TraceId {TraceId}", 
            actionName, traceId);

        if (context.Result is ViewResult)
        {
            _logger.Debug("Skipping standardization for ViewResult in {ActionName}", actionName);
            return;
        }

        if (context.Exception is not null)
        {
            _logger.Warning("Exception occurred in {ActionName}, skipping response standardization. TraceId: {TraceId}", 
                actionName, traceId);
            return;
        }

        if (!context.ModelState.IsValid)
        {
            _logger.Warning("Model validation failed for {ActionName}. TraceId: {TraceId}, Errors: {@ValidationErrors}", 
                actionName, traceId, context.ModelState.ToDictionary(x => x.Key, x => x.Value?.Errors?.Select(s => s.ErrorMessage).ToArray() ?? []));
                
            context.Result = new BadRequestObjectResult(new StandardApiModel
            {
                Status = (int)HttpStatusCode.BadRequest,
                TraceId = traceId,
                ValidationErrors = context.ModelState.ToDictionary(x => x.Key, x => x.Value?.Errors?.Select(s => s.ErrorMessage).ToArray() ?? []),
                ErrorType = ResponseErrorType.ValidationError,
                ActionId = actionId
            });
            context.ExceptionHandled = true;
            context.HttpContext.Response.Headers.Append("Result-Standardized", "true");
            base.OnActionExecuted(context);
            return;
        }

        if (context.Result is UnauthorizedResult || context.Result is UnauthorizedObjectResult)
        {
            _logger.Warning("Unauthorized result for {ActionName}. TraceId: {TraceId}", actionName, traceId);
            
            context.Result = new UnauthorizedObjectResult(new StandardApiModel
            {
                Result = context.Result is ObjectResult oo ? oo.Value : null,
                Status = (int)HttpStatusCode.Unauthorized,
                TraceId = traceId,
                ActionId = actionId
            });
        }
        else
        {
            _logger.Debug("Standardizing successful response for {ActionName}. TraceId: {TraceId}", actionName, traceId);
            
            context.Result = new OkObjectResult(new StandardApiModel
            {
                Result = context.Result is ObjectResult or ? or.Value : null,
                Status = (int)HttpStatusCode.OK,
                TraceId = traceId,
                ActionId = actionId
            });
        }

        context.HttpContext.Response.Headers.Append("Result-Standardized", "true");

        base.OnActionExecuted(context);
    }

}
